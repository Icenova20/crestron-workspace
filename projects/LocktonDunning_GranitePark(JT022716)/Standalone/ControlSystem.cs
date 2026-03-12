using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using LocktonLogic;

namespace LocktonStandalone
{
    /// <summary>
    /// Pure SIMPL# Pro Control System Entry Point.
    /// Orchestrates all rooms and direct hardware without SIMPL Windows.
    /// </summary>
    public class ControlSystem : CrestronControlSystem
    {
        private HardwareRegistry _hardware;
        private RoomManager _rooms;
        private CiscoIntegration _cisco;

        public ControlSystem() : base()
        {
            Thread.MaxNumberOfUserThreads = 40;
        }

        public override void InitializeSystem()
        {
            try
            {
                CrestronConsole.PrintLine("┌──────────────────────────────────────────┐");
                CrestronConsole.PrintLine("│ LOCKTON DUNNING - PURE C# SYSTEM READY   │");
                CrestronConsole.PrintLine("└──────────────────────────────────────────┘");

                // 1. Initialize Logic Core
                _rooms = new RoomManager();
                _rooms.InitializeAll();

                // 2. Register Hardware Directly
                _hardware = new HardwareRegistry(this);
                _hardware.RegisterDevices();

                // 3. Initialize Cisco Direct Integration
                _cisco = new CiscoIntegration("10.10.10.50", "admin", "lockton123");
                _cisco.OnMuteStatusChange = (muted) => { _rooms.TrainingRoom.SetCiscoMute(muted); };
                _cisco.OnStandbyStatusChange = (status) => { _rooms.TrainingRoom.SetCiscoStandby(status); };
                _cisco.Connect();

                // 4. Bind Training Room Delegates (Logic → Hardware)
                _rooms.TrainingRoom.DecoderLeftIP = "10.10.10.101";
                _rooms.TrainingRoom.DecoderRightIP = "10.10.10.102";

                _rooms.TrainingRoom.OnShureMute = (muted) => { _hardware.TrainingMxa920.SetMute(muted ? 1 : 0); };
                _rooms.TrainingRoom.OnPlanarPower = (powerOn) => 
                {
                    if (powerOn) { _hardware.TrainingPlanarLeft.PowerOn(); _hardware.TrainingPlanarRight.PowerOn(); }
                    else { _hardware.TrainingPlanarLeft.PowerOff(); _hardware.TrainingPlanarRight.PowerOff(); }
                };
                
                _rooms.TrainingRoom.OnCiscoMuteToggle = () => 
                { 
                    _cisco.SendCommand("xCommand Audio Microphones Mute Toggle"); 
                };

                // Logic → NVX (Training Room)
                _rooms.TrainingRoom.OnNVXRoute = (source, ip) => 
                {
                    CrestronConsole.PrintLine("[System] NVX ROUTE: Source={0} to Decoder={1}", source, ip);
                    // In a full implementation, we'd trigger the NvxRouteManager here.
                };

                // 5. Bind Restroom Delegates (Logic → Hardware)
                _rooms.Restroom9F.OnAnalogFeedback = (join, val) => { _hardware.Restroom9FVolume.SetLevel(val); };
                _rooms.Restroom9F.OnDigitalFeedback = (join, val) => { _hardware.Restroom9FMute.SetState(val ? (ushort)1 : (ushort)0); };

                _rooms.Restroom10F.OnAnalogFeedback = (join, val) => { _hardware.Restroom10FVolume.SetLevel(val); };
                _rooms.Restroom10F.OnDigitalFeedback = (join, val) => { _hardware.Restroom10FMute.SetState(val ? (ushort)1 : (ushort)0); };

                // 6. Bind UI to Logic (Example: Break Room)
                _hardware.BreakRoomPanel.SigChange += (device, args) => 
                {
                    if (args.Sig.Type == eSigType.Bool && args.Sig.BoolValue)
                        _rooms.BreakRoom.HandleDigitalPress(args.Sig.Number);
                    else if (args.Sig.Type == eSigType.UShort)
                        _rooms.BreakRoom.HandleAnalogChange(args.Sig.Number, args.Sig.UShortValue);
                };

                // Logic Feedback → Panel
                _rooms.BreakRoom.OnDigitalFeedback = (join, val) => { _hardware.BreakRoomPanel.BooleanInput[join].BoolValue = val; };
                _rooms.BreakRoom.OnAnalogFeedback = (join, val) => { _hardware.BreakRoomPanel.UShortInput[join].UShortValue = val; };

                CrestronConsole.PrintLine("[System] Standalone C# Orchestration Online.");
            }
            catch (Exception ex)
            {
                ErrorLog.Error("[ControlSystem] Init error: {0}", ex.Message);
            }
        }
    }
}
