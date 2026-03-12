/**
 * Lockton Dunning Benefits - Break Room 09.002
 * Static Join Map Constants
 * 
 * Source of truth: ch5-workspace/.../js/app.js
 * Must stay in sync with the CH5 UI join definitions.
 */

namespace LocktonTest
{
    public static class JoinMap
    {
        // ── System ──────────────────────────────────────
        public const uint PowerOff       = 10;
        public const uint SettingsModal  = 11;

        // ── Source Selection (Digital, Mutually Exclusive) ──
        public const uint AirMedia       = 21;
        public const uint MediaPlayer    = 22;

        public static readonly uint[] AllSources = { AirMedia, MediaPlayer };

        // ── Volume (Analog Levels) ──────────────────────
        public const uint MasterLevel    = 1;
        public const uint RestroomLevel  = 2;
        public const uint HandheldLevel  = 13;
        public const uint BodypackLevel  = 12;

        // ── Mute (Digital Toggles) ──────────────────────
        public const uint MasterMute     = 1;
        public const uint RestroomMute   = 2;
        public const uint HandheldMute   = 13;
        public const uint BodypackMute   = 12;

        // ── Sonos Transport (Digital Pulses) ────────────
        public const uint SonosPrev      = 41;
        public const uint SonosPlayPause = 42;
        public const uint SonosNext      = 43;

        // ── Scheduling: Auto Power On (Digital, Mutually Exclusive) ──
        public const uint AutoOn7am      = 101;
        public const uint AutoOn8am      = 102;
        public const uint AutoOn9am      = 103;
        public const uint AutoOnDisable  = 104;

        public static readonly uint[] AllAutoOn = { AutoOn7am, AutoOn8am, AutoOn9am, AutoOnDisable };

        // ── Scheduling: Auto Power Off (Digital, Mutually Exclusive) ──
        public const uint AutoOff5pm     = 111;
        public const uint AutoOff6pm     = 112;
        public const uint AutoOff7pm     = 113;
        public const uint AutoOffDisable = 114;

        public static readonly uint[] AllAutoOff = { AutoOff5pm, AutoOff6pm, AutoOff7pm, AutoOffDisable };

        // ── Logic Outputs (Matches USP Outputs) ──────────
        public const uint LogicReady     = 24;
        public const uint DisplayPowerOn = 37;
        public const uint DisplayPowerOff = 38;
        public const uint NVX_DoRoute    = 36;
        
        // ── Serial Feedback ─────────────────────────────
        public const uint RouteNameString = 36;
        public const uint DecoderIPString = 37;

        // ── Scheduling Feedback (Specific Joins) ────────
        // USP handles these as mutual exclusive but they have specific joins
        public const uint SchedOn7am_fb  = 26;
        public const uint SchedOn8am_fb  = 27;
        public const uint SchedOn9am_fb  = 28;
        public const uint SchedOnDisable_fb = 29;
        public const uint SchedOff5pm_fb = 31;
        public const uint SchedOff6pm_fb = 32;
        public const uint SchedOff7pm_fb = 33;
        public const uint SchedOffDisable_fb = 34;
    }
}
