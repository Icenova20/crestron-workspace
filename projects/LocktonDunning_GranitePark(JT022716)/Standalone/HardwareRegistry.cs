using System;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.UI;

namespace LocktonStandalone
{
    /// <summary>
    /// Static registry for physical hardware devices in the pure C# system.
    /// No SIMPL Windows file is required for these registrations.
    /// </summary>
    public class HardwareRegistry
    {
        private readonly CrestronControlSystem _system;

        // UI Panels
        public Tsw1070 BreakRoomPanel { get; private set; }
        public XpanelForSmartGraphics XPanel { get; private set; }

        // AV Drivers (from .clz)
        public ShureMxa920.Mxa920Driver TrainingMxa920 { get; private set; }
        public PlanarDisplay.DisplayDriver TrainingPlanarLeft { get; private set; }
        public PlanarDisplay.DisplayDriver TrainingPlanarRight { get; private set; }
        
        // Biamp Tesira (Multi-Zone)
        public BiampTesiraLib3.CommandProcessor TesiraProcessor { get; private set; }
        public BiampTesiraLib3.LevelComponent BreakRoomVolume { get; private set; }
        public BiampTesiraLib3.StateComponent BreakRoomMute { get; private set; }
        
        public BiampTesiraLib3.LevelComponent Restroom9FVolume { get; private set; }
        public BiampTesiraLib3.StateComponent Restroom9FMute { get; private set; }
        
        public BiampTesiraLib3.LevelComponent Restroom10FVolume { get; private set; }
        public BiampTesiraLib3.StateComponent Restroom10FMute { get; private set; }

        public HardwareRegistry(CrestronControlSystem system)
        {
            _system = system;
        }

        public void RegisterDevices()
        {
            // --- UI ---
            BreakRoomPanel = new Tsw1070(0x07, _system);
            BreakRoomPanel.Register();

            // --- DRIVERS ---
            TrainingMxa920 = new ShureMxa920.Mxa920Driver();
            TrainingMxa920.Initialize("10.10.10.60");
            TrainingMxa920.Connect();

            TrainingPlanarLeft = new PlanarDisplay.DisplayDriver();
            TrainingPlanarLeft.Initialize("10.10.10.71");

            TrainingPlanarRight = new PlanarDisplay.DisplayDriver();
            TrainingPlanarRight.Initialize("10.10.10.72");

            // --- BIAMP TESIRA ---
            TesiraProcessor = new BiampTesiraLib3.CommandProcessor();
            TesiraProcessor.Initialize("10.10.10.40", 2000); // Production Biamp IP
            
            BreakRoomVolume = new BiampTesiraLib3.LevelComponent();
            BreakRoomVolume.Configure(TesiraProcessor, "BreakRoom_Vol", 1, 0);
            
            BreakRoomMute = new BiampTesiraLib3.StateComponent();
            BreakRoomMute.Configure(TesiraProcessor, "BreakRoom_Mute", 1, 0);

            Restroom9FVolume = new BiampTesiraLib3.LevelComponent();
            Restroom9FVolume.Configure(TesiraProcessor, "Restroom9F_Vol", 1, 0);
            
            Restroom9FMute = new BiampTesiraLib3.StateComponent();
            Restroom9FMute.Configure(TesiraProcessor, "Restroom9F_Mute", 1, 0);

            Restroom10FVolume = new BiampTesiraLib3.LevelComponent();
            Restroom10FVolume.Configure(TesiraProcessor, "Restroom10F_Vol", 1, 0);
            
            Restroom10FMute = new BiampTesiraLib3.StateComponent();
            Restroom10FMute.Configure(TesiraProcessor, "Restroom10F_Mute", 1, 0);

            CrestronConsole.PrintLine("[Hardware] Shure, Planar, and multi-zone Biamp drivers initialized.");
        }
    }
}
