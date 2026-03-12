/**
 * Lockton Dunning Benefits - Break Room 09.002
 * Panel Manager
 * 
 * Abstraction layer for the Crestron touch panel (TSW-1070 or XPanel).
 * Handles SigEventHandler wiring and provides clean methods for
 * setting digital/analog feedback.
 */

using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;

namespace LocktonTest
{
    public class PanelManager
    {
        private readonly BasicTriListWithSmartObject _panel;
        private RoomLogic _logic;

        public PanelManager(BasicTriListWithSmartObject panel)
        {
            _panel = panel;
        }

        /// <summary>
        /// Attach the room logic controller. Must be called before the panel
        /// receives any events.
        /// </summary>
        public void AttachLogic(RoomLogic logic)
        {
            _logic = logic;
        }

        /// <summary>
        /// Wire up all SigChange event handlers on the panel.
        /// Call this after the panel is registered.
        /// </summary>
        public void Initialize()
        {
            _panel.SigChange += Panel_SigChange;
            _panel.OnlineStatusChange += Panel_OnlineStatusChange;
            CrestronConsole.PrintLine("[PanelMgr] Event handlers wired for {0}", _panel.Name);
        }

        // ── Feedback Methods ────────────────────────────

        /// <summary>
        /// Set a digital output (feedback) on the panel.
        /// </summary>
        public void SetDigitalFeedback(uint join, bool value)
        {
            try
            {
                _panel.BooleanInput[join].BoolValue = value;
            }
            catch (Exception ex)
            {
                ErrorLog.Error("[PanelMgr] SetDigitalFeedback error join {0}: {1}", join, ex.Message);
            }
        }

        /// <summary>
        /// Set an analog output (feedback) on the panel.
        /// </summary>
        public void SetAnalogFeedback(uint join, ushort value)
        {
            try
            {
                _panel.UShortInput[join].UShortValue = value;
            }
            catch (Exception ex)
            {
                ErrorLog.Error("[PanelMgr] SetAnalogFeedback error join {0}: {1}", join, ex.Message);
            }
        }

        /// <summary>
        /// Set a serial output (feedback) on the panel.
        /// </summary>
        public void SetSerialFeedback(uint join, string value)
        {
            try
            {
                _panel.StringInput[join].StringValue = value;
            }
            catch (Exception ex)
            {
                ErrorLog.Error("[PanelMgr] SetSerialFeedback error join {0}: {1}", join, ex.Message);
            }
        }

        // ── Query Methods (for TestBridge) ──────────────

        /// <summary>
        /// Read the current digital feedback state for a join.
        /// </summary>
        public bool GetDigitalFeedback(uint join)
        {
            try
            {
                return _panel.BooleanInput[join].BoolValue;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Read the current analog feedback value for a join.
        /// </summary>
        public ushort GetAnalogFeedback(uint join)
        {
            try
            {
                return _panel.UShortInput[join].UShortValue;
            }
            catch
            {
                return 0;
            }
        }

        // ── Event Handlers ──────────────────────────────

        private void Panel_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            if (_logic == null)
            {
                CrestronConsole.PrintLine("[PanelMgr] WARNING: Logic not attached, ignoring signal");
                return;
            }

            try
            {
                switch (args.Sig.Type)
                {
                    case eSigType.Bool:
                        if (args.Sig.BoolValue) // Rising edge only (press)
                        {
                            CrestronConsole.PrintLine("[PanelMgr] Digital IN  → join {0} HIGH", args.Sig.Number);
                            _logic.HandleDigitalPress(args.Sig.Number);
                        }
                        break;

                    case eSigType.UShort:
                        CrestronConsole.PrintLine("[PanelMgr] Analog IN  → join {0} = {1}", args.Sig.Number, args.Sig.UShortValue);
                        _logic.HandleAnalogChange(args.Sig.Number, args.Sig.UShortValue);
                        break;

                    case eSigType.String:
                        CrestronConsole.PrintLine("[PanelMgr] Serial IN  → join {0} = \"{1}\"", args.Sig.Number, args.Sig.StringValue);
                        break;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Error("[PanelMgr] SigChange error: {0}", ex.Message);
            }
        }

        private void Panel_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            CrestronConsole.PrintLine("[PanelMgr] Panel {0} is now {1}",
                currentDevice.Name,
                args.DeviceOnLine ? "ONLINE" : "OFFLINE");

            if (args.DeviceOnLine)
            {
                // Send initial state when panel comes online
                SendInitialState();
            }
        }

        /// <summary>
        /// Push default state to the panel when it first connects.
        /// Sets scheduling defaults and clears all source/mute feedbacks.
        /// </summary>
        private void SendInitialState()
        {
            CrestronConsole.PrintLine("[PanelMgr] Sending initial state to panel...");

            // Clear sources
            foreach (uint src in JoinMap.AllSources)
            {
                SetDigitalFeedback(src, false);
            }

            // Default scheduling: Auto On = Disable, Auto Off = Disable
            foreach (uint j in JoinMap.AllAutoOn)
            {
                SetDigitalFeedback(j, j == JoinMap.AutoOnDisable);
            }
            foreach (uint j in JoinMap.AllAutoOff)
            {
                SetDigitalFeedback(j, j == JoinMap.AutoOffDisable);
            }

            // Clear mutes
            SetDigitalFeedback(JoinMap.MasterMute, false);
            SetDigitalFeedback(JoinMap.RestroomMute, false);
            SetDigitalFeedback(JoinMap.HandheldMute, false);
            SetDigitalFeedback(JoinMap.BodypackMute, false);

            // Zero volumes
            SetAnalogFeedback(JoinMap.MasterLevel, 0);
            SetAnalogFeedback(JoinMap.RestroomLevel, 0);
            SetAnalogFeedback(JoinMap.HandheldLevel, 0);
            SetAnalogFeedback(JoinMap.BodypackLevel, 0);
        }
    }
}
