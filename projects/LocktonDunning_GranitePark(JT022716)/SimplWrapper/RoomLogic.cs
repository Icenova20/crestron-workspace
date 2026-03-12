/**
 * Lockton Dunning Benefits - Break Room 09.002
 * Production Room Logic Controller
 * 
 * Implements the core room state machine: source switching, volume/mute,
 * scheduling, and power management.
 * 
 * Target: CP4/RMC4 (SIMPL# Pro)
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

        private readonly CTimer _schedTimer;
        private string _nvxDecoderIP = "192.168.1.100"; // Default, should be config-driven

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

            // Start Scheduling Heartbeat (Check every minute)
            _schedTimer = new CTimer(OnSchedPulse, null, 60000, 60000);
        }

        public void Initialize()
        {
            _panel.SetDigitalFeedback(JoinMap.LogicReady, true);
            CrestronConsole.PrintLine("[RoomLogic] Logic Initialized and Ready.");
        }

        public void SetConfig(string decoderIp)
        {
            _nvxDecoderIP = decoderIp;
        }

        // ── Scheduling Engine ───────────────────────────

        private void OnSchedPulse(object state)
        {
            var now = DateTime.Now;
            int hour = now.Hour;
            int minute = now.Minute;

            // Only check on the top of the minute, though the timer is 60s
            // it might drift, so we just check "now".

            // Auto-On Logic
            if (_activeAutoOn != JoinMap.AutoOnDisable)
            {
                bool triggerOn = false;
                if (_activeAutoOn == JoinMap.AutoOn7am && hour == 7 && minute == 0) triggerOn = true;
                else if (_activeAutoOn == JoinMap.AutoOn8am && hour == 8 && minute == 0) triggerOn = true;
                else if (_activeAutoOn == JoinMap.AutoOn9am && hour == 9 && minute == 0) triggerOn = true;

                if (triggerOn)
                {
                    CrestronConsole.PrintLine("[Sched] AUTO-ON Triggered at {0:D2}:{1:D2}", hour, minute);
                    SelectSource(JoinMap.AirMedia); // Default to AirMedia on Auto-On
                }
            }

            // Auto-Off Logic
            if (_activeAutoOff != JoinMap.AutoOffDisable)
            {
                bool triggerOff = false;
                if (_activeAutoOff == JoinMap.AutoOff5pm && hour == 17 && minute == 0) triggerOff = true;
                else if (_activeAutoOff == JoinMap.AutoOff6pm && hour == 18 && minute == 0) triggerOff = true;
                else if (_activeAutoOff == JoinMap.AutoOff7pm && hour == 19 && minute == 0) triggerOff = true;

                if (triggerOff)
                {
                    CrestronConsole.PrintLine("[Sched] AUTO-OFF Triggered at {0:D2}:{1:D2}", hour, minute);
                    PowerOff();
                }
            }
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
                    _panel.SetDigitalFeedback(JoinMap.SettingsModal, true); // Visual feedback
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

            // Enforce mutual exclusivity on Panel
            foreach (uint src in JoinMap.AllSources)
            {
                _panel.SetDigitalFeedback(src, src == join);
            }

            // Power On Display
            _panel.SetDigitalFeedback(JoinMap.DisplayPowerOn, true);
            _panel.SetDigitalFeedback(JoinMap.DisplayPowerOff, false);

            // Execute NVX Route (Matches USP logic)
            string sourceName = (join == JoinMap.AirMedia) ? "AirMedia" : "MediaPlayer";
            _panel.SetSerialFeedback(JoinMap.RouteNameString, sourceName);
            _panel.SetSerialFeedback(JoinMap.DecoderIPString, _nvxDecoderIP);
            
            // Pulse DoRoute (High then Low after 500ms)
            _panel.SetDigitalFeedback(JoinMap.NVX_DoRoute, true);
            new CTimer((obj) => { _panel.SetDigitalFeedback(JoinMap.NVX_DoRoute, false); }, 500);

            CrestronConsole.PrintLine("[RoomLogic] Source selected: {0}, Routing to: {1}", sourceName, _nvxDecoderIP);
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
            // USP pulses logic outputs for Sonos. We can mirror that or call real API.
            // For feature parity, we'll pulse the panel joins so external listeners (drivers) can react.
            _panel.SetDigitalFeedback(join, true);
            new CTimer((obj) => { _panel.SetDigitalFeedback(join, false); }, 250);

            string action = (join == JoinMap.SonosPrev) ? "Prev" : (join == JoinMap.SonosPlayPause ? "Play/Pause" : "Next");
            CrestronConsole.PrintLine("[RoomLogic] Sonos Executed: {0}", action);
        }

        // ── Scheduling ─────────────────────────────────

        private void SelectAutoOn(uint join)
        {
            _activeAutoOn = join;

            // Enforce mutual exclusivity on Panel
            foreach (uint j in JoinMap.AllAutoOn)
            {
                _panel.SetDigitalFeedback(j, j == join);
            }

            CrestronConsole.PrintLine("[RoomLogic] Auto-On set to join {0}", join);
        }

        private void SelectAutoOff(uint join)
        {
            _activeAutoOff = join;

            // Enforce mutual exclusivity on Panel
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

            // Power Off Display
            _panel.SetDigitalFeedback(JoinMap.DisplayPowerOn, false);
            _panel.SetDigitalFeedback(JoinMap.DisplayPowerOff, true);

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
