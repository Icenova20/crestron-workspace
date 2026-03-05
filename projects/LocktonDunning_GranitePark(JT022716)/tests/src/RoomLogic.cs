/**
 * Lockton Dunning Benefits - Break Room 09.002
 * Room Logic Controller
 * 
 * Implements the room state machine: source switching, volume/mute,
 * scheduling, and power management. All feedback is echoed back
 * to the panel via the PanelManager.
 * 
 * This is a "simulation" controller — no actual AV hardware is driven.
 * It validates the join contract between the CH5 UI and the processor.
 */

using System;
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace LocktonTest
{
    public class RoomLogic
    {
        private readonly PanelManager _panel;

        // ── State Tracking ──────────────────────────────
        private uint _activeSource;
        private readonly Dictionary<uint, bool> _muteStates;
        private readonly Dictionary<uint, ushort> _volumeLevels;
        private uint _activeAutoOn;
        private uint _activeAutoOff;
        private bool _isSystemOn;

        public RoomLogic(PanelManager panel)
        {
            _panel = panel;

            _muteStates = new Dictionary<uint, bool>
            {
                { JoinMap.MasterMute, false },
                { JoinMap.RestroomMute, false },
                { JoinMap.HandheldMute, false },
                { JoinMap.BodypackMute, false }
            };

            _volumeLevels = new Dictionary<uint, ushort>
            {
                { JoinMap.MasterLevel, 0 },
                { JoinMap.RestroomLevel, 0 },
                { JoinMap.HandheldLevel, 0 },
                { JoinMap.BodypackLevel, 0 }
            };

            _activeSource = 0;
            _activeAutoOn = JoinMap.AutoOnDisable;
            _activeAutoOff = JoinMap.AutoOffDisable;
            _isSystemOn = false;
        }

        // ── Public Getters (for TestBridge queries) ─────

        public bool IsSystemOn { get { return _isSystemOn; } }
        public uint ActiveSource { get { return _activeSource; } }
        public uint ActiveAutoOn { get { return _activeAutoOn; } }
        public uint ActiveAutoOff { get { return _activeAutoOff; } }

        public bool GetMuteState(uint join)
        {
            bool val;
            return _muteStates.TryGetValue(join, out val) ? val : false;
        }

        public ushort GetVolumeLevel(uint join)
        {
            ushort val;
            return _volumeLevels.TryGetValue(join, out val) ? val : (ushort)0;
        }

        // ── Digital Input Handler ───────────────────────

        public void HandleDigitalPress(uint join)
        {
            try
            {
                // Power Off
                if (join == JoinMap.PowerOff)
                {
                    PowerOff();
                    return;
                }

                // Settings Modal (informational only)
                if (join == JoinMap.SettingsModal)
                {
                    CrestronConsole.PrintLine("[RoomLogic] Settings modal opened");
                    return;
                }

                // Source Selection
                if (IsSourceJoin(join))
                {
                    SelectSource(join);
                    return;
                }

                // Mute Toggles
                if (_muteStates.ContainsKey(join))
                {
                    ToggleMute(join);
                    return;
                }

                // Sonos Transport
                if (join == JoinMap.SonosPrev || join == JoinMap.SonosPlayPause || join == JoinMap.SonosNext)
                {
                    HandleSonos(join);
                    return;
                }

                // Scheduling: Auto On
                if (IsAutoOnJoin(join))
                {
                    SelectAutoOn(join);
                    return;
                }

                // Scheduling: Auto Off
                if (IsAutoOffJoin(join))
                {
                    SelectAutoOff(join);
                    return;
                }

                CrestronConsole.PrintLine("[RoomLogic] Unhandled digital join: {0}", join);
            }
            catch (Exception ex)
            {
                ErrorLog.Error("[RoomLogic] HandleDigitalPress error on join {0}: {1}", join, ex.Message);
            }
        }

        // ── Analog Input Handler ────────────────────────

        public void HandleAnalogChange(uint join, ushort value)
        {
            try
            {
                if (_volumeLevels.ContainsKey(join))
                {
                    _volumeLevels[join] = value;
                    _panel.SetAnalogFeedback(join, value);

                    int pct = (int)Math.Round((double)value / 65535.0 * 100.0);
                    CrestronConsole.PrintLine("[RoomLogic] Volume join {0} → {1} ({2}%)", join, value, pct);
                }
                else
                {
                    CrestronConsole.PrintLine("[RoomLogic] Unhandled analog join: {0}", join);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Error("[RoomLogic] HandleAnalogChange error on join {0}: {1}", join, ex.Message);
            }
        }

        // ── Source Switching ────────────────────────────

        private void SelectSource(uint join)
        {
            _isSystemOn = true;
            _activeSource = join;

            // Enforce mutual exclusivity
            foreach (uint src in JoinMap.AllSources)
            {
                _panel.SetDigitalFeedback(src, src == join);
            }

            CrestronConsole.PrintLine("[RoomLogic] Source selected: join {0}", join);
        }

        // ── Mute Toggle ────────────────────────────────

        private void ToggleMute(uint join)
        {
            _muteStates[join] = !_muteStates[join];
            _panel.SetDigitalFeedback(join, _muteStates[join]);
            CrestronConsole.PrintLine("[RoomLogic] Mute join {0} → {1}", join, _muteStates[join] ? "MUTED" : "UNMUTED");
        }

        // ── Sonos ──────────────────────────────────────

        private void HandleSonos(uint join)
        {
            string action = "Unknown";
            if (join == JoinMap.SonosPrev) action = "Previous";
            else if (join == JoinMap.SonosPlayPause) action = "Play/Pause";
            else if (join == JoinMap.SonosNext) action = "Next";

            CrestronConsole.PrintLine("[RoomLogic] Sonos: {0} (join {1})", action, join);
        }

        // ── Scheduling ─────────────────────────────────

        private void SelectAutoOn(uint join)
        {
            _activeAutoOn = join;

            // Enforce mutual exclusivity
            foreach (uint j in JoinMap.AllAutoOn)
            {
                _panel.SetDigitalFeedback(j, j == join);
            }

            CrestronConsole.PrintLine("[RoomLogic] Auto-On set to join {0}", join);
        }

        private void SelectAutoOff(uint join)
        {
            _activeAutoOff = join;

            // Enforce mutual exclusivity
            foreach (uint j in JoinMap.AllAutoOff)
            {
                _panel.SetDigitalFeedback(j, j == join);
            }

            CrestronConsole.PrintLine("[RoomLogic] Auto-Off set to join {0}", join);
        }

        // ── Power Off ──────────────────────────────────

        private void PowerOff()
        {
            _isSystemOn = false;
            _activeSource = 0;

            // Clear all source feedbacks
            foreach (uint src in JoinMap.AllSources)
            {
                _panel.SetDigitalFeedback(src, false);
            }

            // Reset all mutes to unmuted
            var keys = new List<uint>(_muteStates.Keys);
            foreach (uint key in keys)
            {
                _muteStates[key] = false;
                _panel.SetDigitalFeedback(key, false);
            }

            // Reset volumes to 0
            var volKeys = new List<uint>(_volumeLevels.Keys);
            foreach (uint key in volKeys)
            {
                _volumeLevels[key] = 0;
                _panel.SetAnalogFeedback(key, 0);
            }

            CrestronConsole.PrintLine("[RoomLogic] *** SYSTEM POWER OFF — All state reset ***");
        }

        // ── Join Classification Helpers ─────────────────

        private bool IsSourceJoin(uint join)
        {
            foreach (uint src in JoinMap.AllSources)
            {
                if (src == join) return true;
            }
            return false;
        }

        private bool IsAutoOnJoin(uint join)
        {
            foreach (uint j in JoinMap.AllAutoOn)
            {
                if (j == join) return true;
            }
            return false;
        }

        private bool IsAutoOffJoin(uint join)
        {
            foreach (uint j in JoinMap.AllAutoOff)
            {
                if (j == join) return true;
            }
            return false;
        }
    }
}
