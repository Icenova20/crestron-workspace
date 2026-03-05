using System;
using Crestron.SimplSharp;

namespace SonosControl
{
    public delegate void EmptyCallbackHandler();

    public class SonosDriver
    {
        private string _zoneName;
        private string _bridgeHost;
        private int _bridgePort;

        public EmptyCallbackHandler OnVolumeChanged { get; set; }
        public EmptyCallbackHandler OnTransportStateChanged { get; set; }

        public ushort CurrentVolume { get; private set; }
        public string CurrentTransportState { get; private set; }

        public SonosDriver()
        {
        }

        public static SonosDriver Create()
        {
            return new SonosDriver();
        }

        public void Initialize(string zoneName, string bridgeHost, int bridgePort)
        {
            _zoneName = zoneName;
            _bridgeHost = bridgeHost;
            _bridgePort = bridgePort;
        }

        public void Play() { SendCommand("play"); }
        public void Pause() { SendCommand("pause"); }
        public void Next() { SendCommand("next"); }
        public void Previous() { SendCommand("previous"); }

        public void SetVolume(ushort level)
        {
            CrestronConsole.PrintLine("Sonos: {0} Volume -> {1}", _zoneName, level);
            // HTTP logic to bridge
            CurrentVolume = level;
            if (OnVolumeChanged != null) OnVolumeChanged();
        }

        private void SendCommand(string command)
        {
            CrestronConsole.PrintLine("Sonos: Zone {0} Command: {1}", _zoneName, command);
            // HTTP logic to bridge
            CurrentTransportState = command;
            if (OnTransportStateChanged != null) OnTransportStateChanged();
        }
    }
}
