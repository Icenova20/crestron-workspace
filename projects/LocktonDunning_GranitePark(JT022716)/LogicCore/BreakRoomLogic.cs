using System;
using System.Collections.Generic;

namespace LocktonLogic
{
    public class BreakRoomLogic : BaseRoom
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

        public BreakRoomLogic() : base("Break Room 09.002")
        {
            _muteStates = new Dictionary<uint, bool>
            {
                { JoinMap.MasterMute, false },
                { JoinMap.RestroomMute, false },
                { JoinMap.HandheldMute, false },
                { JoinMap.BodypackMute, false }
            };

            _volumeLevels = new Dictionary<uint, ushort>
            {
                { JoinMap.MasterLevel, 0 },
                { JoinMap.RestroomLevel, 0 },
                { JoinMap.HandheldLevel, 0 },
                { JoinMap.BodypackLevel, 0 }
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
        }

        public override void HandleAnalogChange(uint join, ushort value)
        {
            if (_volumeLevels.ContainsKey(join))
            {
                _volumeLevels[join] = value;
                if (OnAnalogFeedback != null) OnAnalogFeedback(join, value);
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
    }
}
