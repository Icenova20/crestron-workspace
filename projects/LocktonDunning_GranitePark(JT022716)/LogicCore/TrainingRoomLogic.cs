using System;

namespace LocktonLogic
{
    public class TrainingRoomLogic : BaseRoom
    {
        // Internal State
        private bool _ciscoMuted;
        private string _ciscoActiveSource;
        private string _ciscoStandbyState;

        // Hardware Hooks
        public Action<bool> OnPlanarPower;
        public Action<bool> OnShureMute;
        public Action OnCiscoMuteToggle; // New: Trigger Cisco Mute Toggle
        public Action<string, string> OnNVXRoute; // SourceName, DecoderIP

        public string DecoderLeftIP { get; set; }
        public string DecoderRightIP { get; set; }

        public TrainingRoomLogic() : base("Training Room 09.091")
        {
            _ciscoStandbyState = "Standby";
        }

        public override void Initialize() { }

        // --- Hardware Event Sink (From Standalone / SIMPL) ---

        public void SetCiscoMute(bool muted)
        {
            _ciscoMuted = muted;
            if (OnShureMute != null) OnShureMute(muted);
        }

        public void SetCiscoSource(string source)
        {
            _ciscoActiveSource = source;
            if (source == "Wallplate" || source == "AirMedia")
                TriggerNVXRoute(source);
        }

        public void SetCiscoStandby(string status)
        {
            _ciscoStandbyState = status;
            bool powerOn = (status == "Off" || status == "Halfwake");
            if (OnPlanarPower != null) OnPlanarPower(powerOn);
        }

        public void HandleMicButton()
        {
            if (OnCiscoMuteToggle != null) OnCiscoMuteToggle();
        }

        private void TriggerNVXRoute(string source)
        {
            if (OnNVXRoute != null)
            {
                if (!string.IsNullOrEmpty(DecoderLeftIP)) OnNVXRoute(source, DecoderLeftIP);
                if (!string.IsNullOrEmpty(DecoderRightIP)) OnNVXRoute(source, DecoderRightIP);
            }
        }

        // --- BaseRoom Requirements (If UI were added later) ---
        public override void HandleDigitalPress(uint join) { }
        public override void HandleAnalogChange(uint join, ushort value) { }
    }
}
