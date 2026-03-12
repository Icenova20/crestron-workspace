using System;
using System.Collections.Generic;

namespace LocktonLogic
{
    public class HospitalityLogic : BaseRoom
    {
        private readonly Dictionary<uint, bool> _muteStates;
        private readonly Dictionary<uint, ushort> _volumeLevels;
        private uint _activeSource;
        private uint _activeAutoOn;
        private uint _activeAutoOff;

        // Hooks for hardware/UI feedback
        public Action<uint, bool> OnDigitalFeedback;
        public Action<uint, ushort> OnAnalogFeedback;
        public Action<uint, string> OnSerialFeedback;
        public Action<uint> OnPulse;

        public HospitalityLogic() : base("Hospitality Area 14.086")
        {
            _muteStates = new Dictionary<uint, bool>
            {
                { JoinMap.MasterMute, false },
                { JoinMap.RestroomMute, false },
                { JoinMap.Handheld1Mute, false },
                { JoinMap.Handheld2Mute, false },
                { JoinMap.Lapel1Mute, false },
                { JoinMap.Lapel2Mute, false }
            };

            _volumeLevels = new Dictionary<uint, ushort>
            {
                { JoinMap.MasterLevel, 0 },
                { JoinMap.RestroomLevel, 0 },
                { JoinMap.Handheld1Level, 0 },
                { JoinMap.Handheld2Level, 0 },
                { JoinMap.Lapel1Level, 0 },
                { JoinMap.Lapel2Level, 0 }
            };

            _activeSource = JoinMap.SourceOff;
            _activeAutoOn = JoinMap.AutoOnDisable;
            _activeAutoOff = JoinMap.AutoOffDisable;
        }

        public override void Initialize()
        {
            if (OnDigitalFeedback != null) OnDigitalFeedback(JoinMap.LogicReady, true);
        }

        public override void HandleDigitalPress(uint join)
        {
            if (join == JoinMap.PowerOff) PowerOff();
            else if (IsSource(join)) SelectSource(join);
            else if (_muteStates.ContainsKey(join)) ToggleMute(join);
            else if (join == JoinMap.SonosPrev || join == JoinMap.SonosPlayPause || join == JoinMap.SonosNext)
            {
                if (OnPulse != null) OnPulse(join);
            }
            else if (IsAutoOn(join)) SelectAutoOn(join);
            else if (IsAutoOff(join)) SelectAutoOff(join);
        }

        public override void HandleAnalogChange(uint join, ushort value)
        {
            if (_volumeLevels.ContainsKey(join))
            {
                _volumeLevels[join] = value;
                // Notify EVERYTHING (UI and Hardware)
                if (OnAnalogFeedback != null) OnAnalogFeedback(join, value);
            }
        }

        /// <summary>
        /// Sync value from hardware feedback (Updates Logic and UI, skip hardware set)
        /// </summary>
        public void SyncAnalogValue(uint join, ushort value)
        {
            if (_volumeLevels.ContainsKey(join) && _volumeLevels[join] != value)
            {
                _volumeLevels[join] = value;
                if (OnAnalogFeedback != null) OnAnalogFeedback(join, value);
            }
        }

        /// <summary>
        /// Sync state from hardware feedback (Updates Logic and UI, skip hardware set)
        /// </summary>
        public void SyncDigitalValue(uint join, bool value)
        {
            if (_muteStates.ContainsKey(join) && _muteStates[join] != value)
            {
                _muteStates[join] = value;
                if (OnDigitalFeedback != null) OnDigitalFeedback(join, value);
            }
        }

        private void SelectSource(uint join)
        {
            IsSystemOn = (join != JoinMap.SourceOff);
            _activeSource = join;

            foreach (uint src in JoinMap.AllSources)
            {
                if (OnDigitalFeedback != null) OnDigitalFeedback(src, src == join);
            }

            if (IsSystemOn)
            {
                if (OnDigitalFeedback != null) OnDigitalFeedback(JoinMap.DisplayPowerOn, true);
                if (OnDigitalFeedback != null) OnDigitalFeedback(JoinMap.DisplayPowerOff, false);
            }
        }

        private void SelectAutoOn(uint join)
        {
            _activeAutoOn = join;
            foreach (uint s in JoinMap.AllAutoOn)
            {
                if (OnDigitalFeedback != null) OnDigitalFeedback(s, s == join);
            }
        }

        private void SelectAutoOff(uint join)
        {
            _activeAutoOff = join;
            foreach (uint s in JoinMap.AllAutoOff)
            {
                if (OnDigitalFeedback != null) OnDigitalFeedback(s, s == join);
            }
        }

        /// <summary>
        /// Logic Heartbeat (Called every 60 seconds)
        /// Performs scheduled power events.
        /// </summary>
        public void ExecuteHeartbeat(int hour, int minute)
        {
            // Scheduling Check
            if (_activeAutoOn == JoinMap.AutoOn7am && hour == 7 && minute == 0) SelectSource(JoinMap.AirMedia);
            else if (_activeAutoOn == JoinMap.AutoOn8am && hour == 8 && minute == 0) SelectSource(JoinMap.AirMedia);
            else if (_activeAutoOn == JoinMap.AutoOn9am && hour == 9 && minute == 0) SelectSource(JoinMap.AirMedia);

            if (_activeAutoOff == JoinMap.AutoOff5pm && hour == 17 && minute == 0) PowerOff();
            else if (_activeAutoOff == JoinMap.AutoOff6pm && hour == 18 && minute == 0) PowerOff();
            else if (_activeAutoOff == JoinMap.AutoOff7pm && hour == 19 && minute == 0) PowerOff();
        }

        /// <summary>
        /// Update Sonos Metadata (Skip hardware, update logic/UI)
        /// </summary>
        public void SyncSerialValue(uint join, string value)
        {
            if (OnSerialFeedback != null) OnSerialFeedback(join, value);
        }

        private void ToggleMute(uint join)
        {
            _muteStates[join] = !_muteStates[join];
            if (OnDigitalFeedback != null) OnDigitalFeedback(join, _muteStates[join]);
        }

        public override void PowerOff()
        {
            base.PowerOff();
            SelectSource(JoinMap.SourceOff);
            if (OnDigitalFeedback != null) OnDigitalFeedback(JoinMap.DisplayPowerOn, false);
            if (OnDigitalFeedback != null) OnDigitalFeedback(JoinMap.DisplayPowerOff, true);
            
            // Clear mutes
            var keys = new List<uint>(_muteStates.Keys);
            foreach (var key in keys)
            {
                _muteStates[key] = false;
                if (OnDigitalFeedback != null) OnDigitalFeedback(key, false);
            }
        }

        private bool IsSource(uint join)
        {
            foreach (uint src in JoinMap.AllSources) if (src == join) return true;
            return false;
        }

        private bool IsAutoOn(uint join)
        {
            foreach (uint s in JoinMap.AllAutoOn) if (s == join) return true;
            return false;
        }

        private bool IsAutoOff(uint join)
        {
            foreach (uint s in JoinMap.AllAutoOff) if (s == join) return true;
            return false;
        }
    }
}
