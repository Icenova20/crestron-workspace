using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.UI;
using Crestron.SimplSharpPro.GeneralIO;
using BiampTesiraLib3;
using BiampTesiraLib3.Components;

namespace LocktonStandalone
{
    public class HardwareRegistry
    {
        private readonly CrestronControlSystem _system;

        // UI Panels
        public Tsw1070 HospitalityPanel { get; private set; }

        // AV Drivers (from .clz)
        public ShureMxa920.Mxa920Driver TrainingMxa920 { get; private set; }
        public PlanarDisplay.DisplayDriver TrainingPlanarLeft { get; private set; }
        public PlanarDisplay.DisplayDriver TrainingPlanarRight { get; private set; }
        public PlanarDisplay.DisplayDriver TrainingPlanarSmall { get; private set; }
        
        // Cresnet Devices
        public GlsPartCn TrainingPartitionSensor { get; private set; }
        
        // Sonos (from .clz)
        public SonosControl.SonosDriver HospitalitySonos { get; private set; }
        public SonosControl.SonosDriver RestroomSonos { get; private set; }
        
        // Biamp Tesira (Multi-Zone)
        public BiampTesira TesiraProcessor { get; private set; }
        public LevelComponent HospitalityVolume { get; private set; }
        public StateComponent HospitalityMute { get; private set; }
        
        public LevelComponent Restroom14FVolume { get; private set; }
        public StateComponent Restroom14FMute { get; private set; }
        
        public LevelComponent Restroom15FVolume { get; private set; }
        public StateComponent Restroom15FMute { get; private set; }
 
        public LevelComponent MicHH1Volume { get; private set; }
        public StateComponent MicHH1Mute { get; private set; }
        public LevelComponent MicHH2Volume { get; private set; }
        public StateComponent MicHH2Mute { get; private set; }
        public LevelComponent MicLapel1Volume { get; private set; }
        public StateComponent MicLapel1Mute { get; private set; }
        public LevelComponent MicLapel2Volume { get; private set; }
        public StateComponent MicLapel2Mute { get; private set; }

        public HardwareRegistry(CrestronControlSystem system)
        {
            _system = system;
        }

        public void RegisterDevices()
        {
            // --- UI ---
            HospitalityPanel = new Tsw1070(0x07, _system);
            HospitalityPanel.Register();

            // --- DRIVERS ---
            TrainingMxa920 = new ShureMxa920.Mxa920Driver();
            TrainingMxa920.Initialize("172.22.40.11");
            TrainingMxa920.Connect();

            TrainingPlanarLeft = new PlanarDisplay.DisplayDriver();
            TrainingPlanarLeft.Initialize("172.22.10.71");

            TrainingPlanarRight = new PlanarDisplay.DisplayDriver();
            TrainingPlanarRight.Initialize("172.22.10.72");

            TrainingPlanarSmall = new PlanarDisplay.DisplayDriver();
            TrainingPlanarSmall.Initialize("172.22.10.73");

            /*
            // --- CRESNET ---
            TrainingPartitionSensor = new GlsPartCn(0x51, _system);
            TrainingPartitionSensor.Register();
            */

            // --- SONOS ---
            HospitalitySonos = new SonosControl.SonosDriver();
            HospitalitySonos.Initialize("Hospitality", "172.22.10.61", 5005);

            RestroomSonos = new SonosControl.SonosDriver();
            RestroomSonos.Initialize("Restroom", "172.22.10.61", 5005); // Both likely talk to the same bridge IP

            // --- BIAMP TESIRA ---
            // ID 1 used to link all components together
            const ushort processorId = 1;
            
            TesiraProcessor = new BiampTesira();
            // Configure(Type: 0=Telnet, 1=SSH, ID, IP, User, Pass)
            // Prioritizing SSH (1) as per user request.
            TesiraProcessor.Configure(1, processorId, "172.22.40.14", "default", "\r");
            
            HospitalityVolume = new LevelComponent();
            HospitalityVolume.Configure(processorId, "Hospitality_Vol", "level", 1, 0, 1);
            
            HospitalityMute = new StateComponent();
            HospitalityMute.Configure(processorId, "Hospitality_Mute", "mute", 1, 0);

            Restroom14FVolume = new LevelComponent();
            Restroom14FVolume.Configure(processorId, "Restroom14F_Vol", "level", 1, 0, 1);
            
            Restroom14FMute = new StateComponent();
            Restroom14FMute.Configure(processorId, "Restroom14F_Mute", "mute", 1, 0);

            Restroom15FVolume = new LevelComponent();
            Restroom15FVolume.Configure(processorId, "Restroom15F_Vol", "level", 1, 0, 1);
            
            Restroom15FMute = new StateComponent();
            Restroom15FMute.Configure(processorId, "Restroom15F_Mute", "mute", 1, 0);
 
            // --- WIRELESS MICS ---
            MicHH1Volume = new LevelComponent();
            MicHH1Mute = new StateComponent();
            MicHH2Volume = new LevelComponent();
            MicHH2Mute = new StateComponent();
            MicLapel1Volume = new LevelComponent();
            MicLapel1Mute = new StateComponent();
            MicLapel2Volume = new LevelComponent();
            MicLapel2Mute = new StateComponent();
 
            // 5. Wireless Microphones (Consolidated to single 'Wireless_Mics' Instance Tag)
            MicHH1Volume.Configure(processorId, "Wireless_Mics", "channelLevel", 1, 0, 1);
            MicHH1Mute.Configure(processorId, "Wireless_Mics", "channelMute", 1, 0);

            MicHH2Volume.Configure(processorId, "Wireless_Mics", "channelLevel", 2, 0, 1);
            MicHH2Mute.Configure(processorId, "Wireless_Mics", "channelMute", 2, 0);

            MicLapel1Volume.Configure(processorId, "Wireless_Mics", "channelLevel", 3, 0, 1);
            MicLapel1Mute.Configure(processorId, "Wireless_Mics", "channelMute", 3, 0);

            MicLapel2Volume.Configure(processorId, "Wireless_Mics", "channelLevel", 4, 0, 1);
            MicLapel2Mute.Configure(processorId, "Wireless_Mics", "channelMute", 4, 0);

            // Establish connection and trigger internal registration
            TesiraProcessor.Connect();
            TesiraProcessor.Initialize(1); // Set Initialize=TRUE

            CrestronConsole.PrintLine("[Hardware] Shure, Planar, and multi-zone Biamp drivers initialized.");
        }
    }
}
