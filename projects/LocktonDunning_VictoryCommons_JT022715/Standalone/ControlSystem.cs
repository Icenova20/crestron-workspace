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
        private CiscoIntegration _largeCisco;
        private CiscoIntegration _smallCisco;
        private CTimer _heartbeatTimer;

        public ControlSystem() : base()
        {
            Thread.MaxNumberOfUserThreads = 40;
        }

        public override void InitializeSystem()
        {
            try
            {
                // Register Test Command FIRST so it exists even if init fails
                CrestronConsole.AddNewConsoleCommand(ExecuteTestTesira, "TESTTESIRA", "Tests connectivity to all Biamp Tesira components", ConsoleAccessLevelEnum.AccessOperator);

                CrestronConsole.PrintLine("┌──────────────────────────────────────────┐");
                CrestronConsole.PrintLine("│ LOCKTON DUNNING - PURE C# SYSTEM READY   │");
                CrestronConsole.PrintLine("└──────────────────────────────────────────┘");

                // 1. Initialize Logic Core
                _rooms = new RoomManager();
                _rooms.InitializeAll();

                // 2. Register Hardware Directly
                _hardware = new HardwareRegistry(this);
                _hardware.RegisterDevices();

                // Add debug hooks AFTER registration (once objects are instantiated)
                try 
                {
                    if (_hardware.TesiraProcessor != null)
                    {
                        _hardware.TesiraProcessor.OnSendDebug += (sender, args) => 
                        {
                            CrestronConsole.PrintLine("[Tesira-DEBUG] {0}", args.Payload);
                        };
                        _hardware.TesiraProcessor.OnClientSocketStatus += (sender, args) => 
                        {
                            CrestronConsole.PrintLine("[Tesira-SOCKET-STATUS] {0}", args.Payload);
                        };
                    }

                    if (_hardware.HospitalityVolume != null)
                    {
                        _hardware.HospitalityVolume.OnLevelChangePercent += (sender, args) => 
                        {
                            CrestronConsole.PrintLine("[Tesira-FB] Hospitality Volume is now: {0}%", args.Payload);
                        };
                    }
                }
                catch (Exception ex)
                {
                    CrestronConsole.PrintLine("[System] Note: Could not bind Tesira debug events: {0}", ex.Message);
                }

                // 3. Initialize Dual Cisco Direct Integration
                _largeCisco = new CiscoIntegration("172.22.10.50", "admin", "lockton123");
                _largeCisco.OnMuteStatusChange = (muted) => { _rooms.TrainingRoom.SetLargeCiscoMute(muted); };
                _largeCisco.OnStandbyStatusChange = (status) => { _rooms.TrainingRoom.SetLargeCiscoStandby(status); };
                _largeCisco.Connect();

                _smallCisco = new CiscoIntegration("172.22.10.51", "admin", "lockton123");
                _smallCisco.OnMuteStatusChange = (muted) => { _rooms.TrainingRoom.SetSmallCiscoMute(muted); };
                _smallCisco.OnStandbyStatusChange = (status) => { _rooms.TrainingRoom.SetSmallCiscoStandby(status); };
                _smallCisco.Connect();

                // 4. Bind Training Room Delegates (Logic → Hardware)
                _rooms.TrainingRoom.DecoderLargeLeftIP = "172.22.30.12";
                _rooms.TrainingRoom.DecoderLargeRightIP = "172.22.30.13";
                _rooms.TrainingRoom.DecoderSmallIP = "172.22.30.11";

                _rooms.TrainingRoom.OnShureMute = (muted) => { _hardware.TrainingMxa920.SetMute(muted ? (ushort)1 : (ushort)0); };
                
                _rooms.TrainingRoom.OnPlanarLargePower = (powerOn) => 
                {
                    if (powerOn) { _hardware.TrainingPlanarLeft.PowerOn(); _hardware.TrainingPlanarRight.PowerOn(); }
                    else { _hardware.TrainingPlanarLeft.PowerOff(); _hardware.TrainingPlanarRight.PowerOff(); }
                };

                _rooms.TrainingRoom.OnPlanarSmallPower = (powerOn) => 
                {
                    if (powerOn) { _hardware.TrainingPlanarSmall.PowerOn(); }
                    else { _hardware.TrainingPlanarSmall.PowerOff(); }
                };
                
                _rooms.TrainingRoom.OnLargeCiscoMuteToggle = () => { _largeCisco.SendCommand("xCommand Audio Microphones Mute Toggle"); };
                _rooms.TrainingRoom.OnSmallCiscoMuteToggle = () => { _smallCisco.SendCommand("xCommand Audio Microphones Mute Toggle"); };

                // Logic → NVX (Training Room)
                _rooms.TrainingRoom.OnNVXRoute = (source, ip) => 
                {
                    CrestronConsole.PrintLine("[System] NVX ROUTE: Source={0} to Decoder={1}", source, ip);
                };

                // 5. Bind Restroom Delegates (Logic → Hardware)
                _rooms.Restroom14F.OnAnalogFeedback = (join, val) => { _hardware.Restroom14FVolume.SetPercent(val); };
                _rooms.Restroom14F.OnDigitalFeedback = (join, val) => { _hardware.Restroom14FMute.SetState(val ? (ushort)1 : (ushort)0); };

                _rooms.Restroom15F.OnAnalogFeedback = (join, val) => { _hardware.Restroom15FVolume.SetPercent(val); };
                _rooms.Restroom15F.OnDigitalFeedback = (join, val) => { _hardware.Restroom15FMute.SetState(val ? (ushort)1 : (ushort)0); };

                // Hardware → Logic (Restrooms)
                _hardware.Restroom14FVolume.OnLevelChangePercent += (sender, args) => { _rooms.Restroom14F.SyncAnalogValue(JoinMap.RestroomLevel, args.Payload); };
                _hardware.Restroom14FMute.OnStateChange += (sender, args) => { _rooms.Restroom14F.SyncDigitalValue(JoinMap.RestroomMute, args.Payload == 1); };

                _hardware.Restroom15FVolume.OnLevelChangePercent += (sender, args) => { _rooms.Restroom15F.SyncAnalogValue(JoinMap.RestroomLevel, args.Payload); };
                _hardware.Restroom15FMute.OnStateChange += (sender, args) => { _rooms.Restroom15F.SyncDigitalValue(JoinMap.RestroomMute, args.Payload == 1); };

                // 6. Bind Hospitality Delegates (Logic → Hardware)
                _hardware.HospitalityPanel.SigChange += (device, args) => 
                {
                    if (args.Sig.Type == eSigType.Bool && args.Sig.BoolValue)
                        _rooms.HospitalityArea.HandleDigitalPress(args.Sig.Number);
                    else if (args.Sig.Type == eSigType.UShort)
                        _rooms.HospitalityArea.HandleAnalogChange(args.Sig.Number, args.Sig.UShortValue);
                };

                _rooms.HospitalityArea.OnAnalogFeedback = (join, val) => 
                {
                    // Logic → UI (Panel)
                    _hardware.HospitalityPanel.UShortInput[join].UShortValue = val;

                    // Logic → Hardware (Tesira)
                    if (join == JoinMap.MasterLevel) _hardware.HospitalityVolume.SetPercent(val);
                    else if (join == JoinMap.Handheld1Level) _hardware.MicHH1Volume.SetPercent(val);
                    else if (join == JoinMap.Handheld2Level) _hardware.MicHH2Volume.SetPercent(val);
                    else if (join == JoinMap.Lapel1Level) _hardware.MicLapel1Volume.SetPercent(val);
                    else if (join == JoinMap.Lapel2Level) _hardware.MicLapel2Volume.SetPercent(val);
                };

                _rooms.HospitalityArea.OnDigitalFeedback = (join, val) => 
                {
                    // Logic → UI (Panel)
                    _hardware.HospitalityPanel.BooleanInput[join].BoolValue = val;

                    // Logic → Hardware (Tesira)
                    if (join == JoinMap.MasterMute) _hardware.HospitalityMute.SetState(val ? (ushort)1 : (ushort)0);
                    else if (join == JoinMap.Handheld1Mute) _hardware.MicHH1Mute.SetState(val ? (ushort)1 : (ushort)0);
                    else if (join == JoinMap.Handheld2Mute) _hardware.MicHH2Mute.SetState(val ? (ushort)1 : (ushort)0);
                    else if (join == JoinMap.Lapel1Mute) _hardware.MicLapel1Mute.SetState(val ? (ushort)1 : (ushort)0);
                    else if (join == JoinMap.Lapel2Mute) _hardware.MicLapel2Mute.SetState(val ? (ushort)1 : (ushort)0);
                };

                // Hardware → Logic (Hospitality)
                _hardware.HospitalityVolume.OnLevelChangePercent += (sender, args) => { _rooms.HospitalityArea.SyncAnalogValue(JoinMap.MasterLevel, args.Payload); };
                _hardware.HospitalityMute.OnStateChange += (sender, args) => { _rooms.HospitalityArea.SyncDigitalValue(JoinMap.MasterMute, args.Payload == 1); };
                
                _hardware.MicHH1Volume.OnLevelChangePercent += (sender, args) => { _rooms.HospitalityArea.SyncAnalogValue(JoinMap.Handheld1Level, args.Payload); };
                _hardware.MicHH1Mute.OnStateChange += (sender, args) => { _rooms.HospitalityArea.SyncDigitalValue(JoinMap.Handheld1Mute, args.Payload == 1); };
                
                _hardware.MicHH2Volume.OnLevelChangePercent += (sender, args) => { _rooms.HospitalityArea.SyncAnalogValue(JoinMap.Handheld2Level, args.Payload); };
                _hardware.MicHH2Mute.OnStateChange += (sender, args) => { _rooms.HospitalityArea.SyncDigitalValue(JoinMap.Handheld2Mute, args.Payload == 1); };
                
                _hardware.MicLapel1Volume.OnLevelChangePercent += (sender, args) => { _rooms.HospitalityArea.SyncAnalogValue(JoinMap.Lapel1Level, args.Payload); };
                _hardware.MicLapel1Mute.OnStateChange += (sender, args) => { _rooms.HospitalityArea.SyncDigitalValue(JoinMap.Lapel1Mute, args.Payload == 1); };
                
                _hardware.MicLapel2Volume.OnLevelChangePercent += (sender, args) => { _rooms.HospitalityArea.SyncAnalogValue(JoinMap.Lapel2Level, args.Payload); };
                _hardware.MicLapel2Mute.OnStateChange += (sender, args) => { _rooms.HospitalityArea.SyncDigitalValue(JoinMap.Lapel2Mute, args.Payload == 1); };

                // 7. Bind Sonos Integration
                _rooms.HospitalityArea.OnPulse = (join) => 
                {
                    if (join == JoinMap.SonosPrev) _hardware.HospitalitySonos.Previous();
                    else if (join == JoinMap.SonosPlayPause) _hardware.HospitalitySonos.Play(); // The library lacks a toggle, defaulting to Play
                    else if (join == JoinMap.SonosNext) _hardware.HospitalitySonos.Next();
                };

                // Sonos Events -> Logic -> UI
                _hardware.HospitalitySonos.OnVolumeChanged += () => { _rooms.HospitalityArea.SyncAnalogValue(JoinMap.SonosLevel, (ushort)_hardware.HospitalitySonos.CurrentVolume); };
                _hardware.HospitalitySonos.OnTransportStateChanged += () => { _rooms.HospitalityArea.SyncSerialValue(JoinMap.SonosTransport, _hardware.HospitalitySonos.CurrentTransportState); };

                // Note: Title, Artist, and ArtUrl are NOT supported by this DLL version.
                // Re-enabling would require a newer library or a different bridge.

                // Relay Serial feedback to UI
                _rooms.HospitalityArea.OnSerialFeedback = (join, val) => { _hardware.HospitalityPanel.StringInput[join].StringValue = val; };

                // 8. Start Heartbeat Timer (Every 60 seconds)
                _heartbeatTimer = new CTimer(OnHeartbeat, null, 60000, 60000);

                CrestronConsole.PrintLine("[System] Standalone C# Orchestration Online.");
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("\n[CRITICAL ERROR] InitializeSystem Failed: {0}", ex.Message);
                CrestronConsole.PrintLine(ex.StackTrace);
            }
        }

        private void OnHeartbeat(object callbackObject)
        {
            var now = DateTime.Now;
            _rooms.HospitalityArea.ExecuteHeartbeat(now.Hour, now.Minute);
        }

        private void ExecuteTestTesira(string command)
        {
            try
            {
                CrestronConsole.PrintLine("\n--- TESIRA AGGRESSIVE PROBE ---");
                
                CrestronConsole.PrintLine("1. Forcing Re-Connect...");
                _hardware.TesiraProcessor.Disconnect();
                Thread.Sleep(500);
                _hardware.TesiraProcessor.Connect();
                
                CrestronConsole.PrintLine("2. Forcing Initialize(1)...");
                _hardware.TesiraProcessor.Initialize(1);
                
                CrestronConsole.PrintLine("3. Waiting for handshake (5 seconds)...");
                Thread.Sleep(5000);

                bool connected = _hardware.TesiraProcessor.IsInitialized();
                CrestronConsole.PrintLine("   Status: IsInitialized={0}", connected);

                CrestronConsole.PrintLine("4. Attempting Component Poll anyway...");
                if (_hardware.HospitalityVolume != null)
                {
                    CrestronConsole.PrintLine("   Polling Hospitality_Vol...");
                    _hardware.HospitalityVolume.PollState();
                    Thread.Sleep(1000);
                    CrestronConsole.PrintLine("   Poll command sent. Waiting for [Tesira-FB] event...");
                }
                else
                {
                    CrestronConsole.PrintLine("   [ERROR] HospitalityVolume component is NULL in HardwareRegistry.");
                }

                CrestronConsole.PrintLine("5. Check console for [Tesira-DEBUG] or [Tesira-SOCKET] traffic.");
                CrestronConsole.PrintLine("-------------------------------\n");
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("[FATAL] TestTesira failed: {0}", ex.Message);
                CrestronConsole.PrintLine(ex.StackTrace);
            }
        }
    }
}
