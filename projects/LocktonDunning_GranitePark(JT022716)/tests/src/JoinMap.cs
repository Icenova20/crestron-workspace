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
    }
}
