using System;

namespace LocktonLogic
{
    public class RestroomLogic : BaseRoom
    {
        private bool _isMuted;
        private ushort _volumeLevel;

        public Action<uint, bool> OnDigitalFeedback;
        public Action<uint, ushort> OnAnalogFeedback;

        public uint MasterMuteJoin { get; set; }
        public uint MasterLevelJoin { get; set; }

        public RestroomLogic(string name) : base(name)
        {
        }

        public override void Initialize() { }

        public override void HandleDigitalPress(uint join)
        {
            if (join == MasterMuteJoin)
            {
                _isMuted = !_isMuted;
                if (OnDigitalFeedback != null) OnDigitalFeedback(join, _isMuted);
            }
        }

        public override void HandleAnalogChange(uint join, ushort value)
        {
            if (join == MasterLevelJoin)
            {
                _volumeLevel = value;
                if (OnAnalogFeedback != null) OnAnalogFeedback(join, value);
            }
        }

        public void SetMute(bool muted)
        {
            _isMuted = muted;
            if (OnDigitalFeedback != null) OnDigitalFeedback(MasterMuteJoin, _isMuted);
        }

        public void SetLevel(ushort level)
        {
            _volumeLevel = level;
            if (OnAnalogFeedback != null) OnAnalogFeedback(MasterLevelJoin, _volumeLevel);
        }

        public void SyncAnalogValue(uint join, ushort value)
        {
            if (join == MasterLevelJoin && _volumeLevel != value)
            {
                _volumeLevel = value;
                if (OnAnalogFeedback != null) OnAnalogFeedback(join, value);
            }
        }

        public void SyncDigitalValue(uint join, bool value)
        {
            if (join == MasterMuteJoin && _isMuted != value)
            {
                _isMuted = value;
                if (OnDigitalFeedback != null) OnDigitalFeedback(join, _isMuted);
            }
        }
    }
}
