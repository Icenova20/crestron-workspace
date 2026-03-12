using System;
using Crestron.SimplSharp;

namespace LocktonLogic
{
    public class TrainingRoomLogic : BaseRoom
    {
        // Internal State
        private bool _largeCiscoMuted;
        private bool _smallCiscoMuted;
        private string _largeCiscoStandby;
        private string _smallCiscoStandby;
        private bool _isCombined;

        // Hardware Hooks
        public Action<bool> OnPlanarLargePower;
        public Action<bool> OnPlanarSmallPower;
        public Action<bool> OnShureMute;
        public Action OnLargeCiscoMuteToggle;
        public Action OnSmallCiscoMuteToggle;
        public Action<string, string> OnNVXRoute; // SourceName, DecoderIP

        // Decoder IPs
        public string DecoderLargeLeftIP { get; set; }
        public string DecoderLargeRightIP { get; set; }
        public string DecoderSmallIP { get; set; }

        public TrainingRoomLogic() : base("Training Room 09.091")
        {
            _largeCiscoStandby = "Standby";
            _smallCiscoStandby = "Standby";
        }

        public override void Initialize() { }

        // --- Hardware Event Sink (From Standalone / SIMPL) ---

        public void SetLargeCiscoMute(bool muted)
        {
            _largeCiscoMuted = muted;
            UpdateMicStatus();
        }

        public void SetSmallCiscoMute(bool muted)
        {
            _smallCiscoMuted = muted;
            UpdateMicStatus();
        }

        private void UpdateMicStatus()
        {
            // Shure Mics mute if EITHER codec is muted (or both in combined mode)
            bool shouldMute = _largeCiscoMuted || _smallCiscoMuted;
            if (OnShureMute != null) OnShureMute(shouldMute);
        }

        public void SetLargeCiscoStandby(string status)
        {
            _largeCiscoStandby = status;
            bool powerOn = (status == "Off" || status == "Halfwake");
            if (OnPlanarLargePower != null) OnPlanarLargePower(powerOn);
        }

        public void SetSmallCiscoStandby(string status)
        {
            _smallCiscoStandby = status;
            bool powerOn = (status == "Off" || status == "Halfwake");
            if (OnPlanarSmallPower != null) OnPlanarSmallPower(powerOn);
        }

        public void SetCombinedState(bool combined)
        {
            _isCombined = combined;
            CrestronConsole.PrintLine("[TrainingRoom] Partition State: {0}", combined ? "COMBINED" : "DIVIDED");
        }

        public void TriggerNVXRoute(string source)
        {
            if (OnNVXRoute == null) return;

            // Simple Logic: Route to relevant displays
            if (!string.IsNullOrEmpty(DecoderLargeLeftIP)) OnNVXRoute(source, DecoderLargeLeftIP);
            if (!string.IsNullOrEmpty(DecoderLargeRightIP)) OnNVXRoute(source, DecoderLargeRightIP);
            
            if (_isCombined && !string.IsNullOrEmpty(DecoderSmallIP))
                OnNVXRoute(source, DecoderSmallIP);
        }

        // --- BaseRoom Overrides ---
        public override void HandleDigitalPress(uint join) { }
        public override void HandleAnalogChange(uint join, ushort value) { }
    }
}
