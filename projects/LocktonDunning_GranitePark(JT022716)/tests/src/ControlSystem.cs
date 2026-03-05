/**
 * Lockton Dunning Benefits - Break Room 09.002
 * SIMPL# Pro Control System Entry Point
 * 
 * Target: Crestron 4-Series Processor (CP4 / RMC4)
 * Panel:  TSW-1070 (IPID 0x03) + XPanel (IPID 0x04)
 * 
 * Console Commands:
 *   TESTALL  - Run the full join test suite
 *   STATUS   - Print current room state
 *   TESTPY   - Launch Python test runner (if loaded)
 */

using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;

namespace LocktonTest
{
    public class ControlSystem : CrestronControlSystem
    {
        // ── Panel Definitions ───────────────────────────
        // IPID 0x07 = TSW-1070 (physical panel) - Using 0x07 to avoid conflict with Slot 1 (0x03)
        // IPID 0x04 = XPanel   (WebXPanel / virtual testing)
        private const uint PANEL_IPID   = 0x07;
        private const uint XPANEL_IPID  = 0x04;

        private Tsw1070 _tsw1070;
        private XpanelForSmartGraphics _xpanel;

        private PanelManager _tswManager;
        private PanelManager _xpanelManager;
        private RoomLogic _roomLogic;
        private TestBridge _testBridge;

        /// <summary>
        /// Constructor — called by the framework on program load.
        /// </summary>
        public ControlSystem() : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;
                CrestronEnvironment.SystemEventHandler += SystemEvent;
                CrestronEnvironment.ProgramStatusEventHandler += ProgramStatusEvent;
            }
            catch (Exception ex)
            {
                ErrorLog.Error("[ControlSystem] Constructor error: {0}", ex.Message);
            }
        }

        /// <summary>
        /// InitializeSystem — called after the constructor.
        /// Register hardware, wire events, register console commands.
        /// </summary>
        public override void InitializeSystem()
        {
            try
            {
                CrestronConsole.PrintLine("");
                CrestronConsole.PrintLine("╔══════════════════════════════════════════════╗");
                CrestronConsole.PrintLine("║  LOCKTON DUNNING - Break Room 09.002         ║");
                CrestronConsole.PrintLine("║  Test System v1.0                            ║");
                CrestronConsole.PrintLine("╚══════════════════════════════════════════════╝");
                CrestronConsole.PrintLine("");

                // ── Register TSW-1070 ───────────────────
                _tsw1070 = new Tsw1070(PANEL_IPID, this);
                if (_tsw1070.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    ErrorLog.Error("[ControlSystem] TSW-1070 registration failed: {0}",
                        _tsw1070.RegistrationFailureReason);
                }
                else
                {
                    CrestronConsole.PrintLine("[Init] TSW-1070 registered at IPID 0x{0:X2}", PANEL_IPID);
                }

                // ── Register XPanel (for WebXPanel testing) ──
                _xpanel = new XpanelForSmartGraphics(XPANEL_IPID, this);
                if (_xpanel.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    ErrorLog.Error("[ControlSystem] XPanel registration failed: {0}",
                        _xpanel.RegistrationFailureReason);
                }
                else
                {
                    CrestronConsole.PrintLine("[Init] XPanel registered at IPID 0x{0:X2}", XPANEL_IPID);
                }

                // ── Wire Up Components ──────────────────
                // Use the TSW-1070 as the primary panel
                _tswManager = new PanelManager(_tsw1070);
                _xpanelManager = new PanelManager(_xpanel);

                // Room logic drives the TSW panel
                // (XPanel mirrors the same logic independently)
                _roomLogic = new RoomLogic(_tswManager);

                _tswManager.AttachLogic(_roomLogic);
                _tswManager.Initialize();

                // Create a separate RoomLogic for XPanel so it works independently
                var xpanelLogic = new RoomLogic(_xpanelManager);
                _xpanelManager.AttachLogic(xpanelLogic);
                _xpanelManager.Initialize();

                // Test bridge uses TSW panel's logic
                _testBridge = new TestBridge(_roomLogic, _tswManager);

                // ── Register Console Commands ───────────
                CrestronConsole.AddNewConsoleCommand(CmdTestAll, "TESTALL",
                    "Run the full Lockton Dunning join test suite", ConsoleAccessLevelEnum.AccessOperator);

                CrestronConsole.AddNewConsoleCommand(CmdStatus, "STATUS",
                    "Print current room state", ConsoleAccessLevelEnum.AccessOperator);

                CrestronConsole.AddNewConsoleCommand(CmdTestPy, "TESTPY",
                    "Launch Python test runner", ConsoleAccessLevelEnum.AccessOperator);

                CrestronConsole.PrintLine("[Init] System initialized. Type TESTALL to run tests.");
                CrestronConsole.PrintLine("");
            }
            catch (Exception ex)
            {
                ErrorLog.Error("[ControlSystem] InitializeSystem error: {0}", ex.Message);
            }
        }

        // ── Console Command Handlers ────────────────────

        private void CmdTestAll(string args)
        {
            _testBridge.RunConsoleTests();
        }

        private void CmdStatus(string args)
        {
            CrestronConsole.PrintLine("");
            CrestronConsole.PrintLine("── Room Status ──────────────────────────");
            CrestronConsole.PrintLine("  System Power: {0}", _roomLogic.IsSystemOn ? "ON" : "OFF");
            CrestronConsole.PrintLine("  Active Source: {0}", _roomLogic.ActiveSource == 0 ? "None" :
                (_roomLogic.ActiveSource == JoinMap.AirMedia ? "AirMedia (d21)" : "MediaPlayer (d22)"));
            CrestronConsole.PrintLine("");
            CrestronConsole.PrintLine("  Master Vol:   {0}  Mute: {1}",
                _roomLogic.GetVolumeLevel(JoinMap.MasterLevel),
                _roomLogic.GetMuteState(JoinMap.MasterMute) ? "YES" : "NO");
            CrestronConsole.PrintLine("  Restroom Vol: {0}  Mute: {1}",
                _roomLogic.GetVolumeLevel(JoinMap.RestroomLevel),
                _roomLogic.GetMuteState(JoinMap.RestroomMute) ? "YES" : "NO");
            CrestronConsole.PrintLine("  Handheld Mic: {0}  Mute: {1}",
                _roomLogic.GetVolumeLevel(JoinMap.HandheldLevel),
                _roomLogic.GetMuteState(JoinMap.HandheldMute) ? "YES" : "NO");
            CrestronConsole.PrintLine("  Bodypack Mic: {0}  Mute: {1}",
                _roomLogic.GetVolumeLevel(JoinMap.BodypackLevel),
                _roomLogic.GetMuteState(JoinMap.BodypackMute) ? "YES" : "NO");
            CrestronConsole.PrintLine("");
            CrestronConsole.PrintLine("  Auto-On:  join {0}", _roomLogic.ActiveAutoOn);
            CrestronConsole.PrintLine("  Auto-Off: join {0}", _roomLogic.ActiveAutoOff);
            CrestronConsole.PrintLine("─────────────────────────────────────────");
            CrestronConsole.PrintLine("");
        }

        private void CmdTestPy(string args)
        {
            _testBridge.RunTests();
        }

        // ── System Events ───────────────────────────────

        private void SystemEvent(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case eSystemEventType.DiskInserted:
                    break;
                case eSystemEventType.DiskRemoved:
                    break;
                case eSystemEventType.Rebooting:
                    CrestronConsole.PrintLine("[System] Rebooting...");
                    break;
            }
        }

        private void ProgramStatusEvent(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case eProgramStatusEventType.Paused:
                    CrestronConsole.PrintLine("[System] Program paused");
                    break;
                case eProgramStatusEventType.Stopping:
                    CrestronConsole.PrintLine("[System] Program stopping");
                    break;
            }
        }
    }
}
