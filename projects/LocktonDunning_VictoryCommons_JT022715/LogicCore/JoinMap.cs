using System;
using System.Collections.Generic;

namespace LocktonLogic
{
    /// <summary>
    /// Static registry for all touch panel joins used across the system.
    /// Organized by functional block.
    /// </summary>
    public static class JoinMap
    {
        // --- SYSTEM ---
        public const uint PowerOff       = 10;
        public const uint SettingsModal  = 11;
        public const uint LogicReady     = 24;

        // --- SOURCE SELECTION (21-23) ---
        public const uint AirMedia       = 21;
        public const uint MediaPlayer    = 22;
        public const uint SourceOff      = 23;
        public static readonly uint[] AllSources = { AirMedia, MediaPlayer, SourceOff };

        // --- AUDIO (1-5) ---
        public const uint MasterLevel    = 1;
        public const uint MasterMute     = 1;
        public const uint RestroomLevel  = 2;
        public const uint RestroomMute   = 2;

        // --- MICROPHONES (51-54) ---
        public const uint Handheld1Level = 51;
        public const uint Handheld1Mute  = 51;
        public const uint Handheld2Level = 52;
        public const uint Handheld2Mute  = 52;
        public const uint Lapel1Level    = 53;
        public const uint Lapel1Mute     = 53;
        public const uint Lapel2Level    = 54;
        public const uint Lapel2Mute     = 54;

        // --- SONOS (41-43) ---
        public const uint SonosPrev      = 41;
        public const uint SonosPlayPause = 42;
        public const uint SonosNext      = 43;

        // --- SCHEDULING (101-114) ---
        public const uint AutoOn7am      = 101;
        public const uint AutoOn8am      = 102;
        public const uint AutoOn9am      = 103;
        public const uint AutoOnDisable  = 104;
        public static readonly uint[] AllAutoOn = { AutoOn7am, AutoOn8am, AutoOn9am, AutoOnDisable };

        public const uint AutoOff5pm     = 111;
        public const uint AutoOff6pm     = 112;
        public const uint AutoOff7pm     = 113;
        public const uint AutoOffDisable = 114;
        public static readonly uint[] AllAutoOff = { AutoOff5pm, AutoOff6pm, AutoOff7pm, AutoOffDisable };

        // --- HARDWARE TRG (Used in Standalone / S+ Logic) ---
        public const uint NVX_DoRoute    = 36;
        public const uint DisplayPowerOn = 37;
        public const uint DisplayPowerOff = 38;
    }
}
