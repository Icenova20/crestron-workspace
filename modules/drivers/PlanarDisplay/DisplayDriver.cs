using System;
using System.Net.Http;
using System.Threading.Tasks;
using Crestron.SimplSharp;

namespace PlanarDisplay
{
    public delegate void EmptyCallbackHandler();

    public class DisplayDriver
    {
        private HttpClient _client;
        private string _ip;

        public EmptyCallbackHandler OnPowerChanged { get; set; }
        public EmptyCallbackHandler OnBacklightChanged { get; set; }

        public ushort CurrentPowerState { get; private set; }
        public ushort CurrentBacklight { get; private set; }

        public DisplayDriver()
        {
        }

        public static DisplayDriver Create()
        {
            return new DisplayDriver();
        }

        public void Initialize(string ip)
        {
            _ip = ip;
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(3);
        }

        public void PowerOn()
        {
            CrestronConsole.PrintLine("Planar: Setting Power ON on {0}", _ip);
            CurrentPowerState = 1;
            if (OnPowerChanged != null) OnPowerChanged();
        }

        public void PowerOff()
        {
            CrestronConsole.PrintLine("Planar: Setting Power OFF on {0}", _ip);
            CurrentPowerState = 0;
            if (OnPowerChanged != null) OnPowerChanged();
        }

        public void SetBacklight(int level)
        {
            CrestronConsole.PrintLine("Planar: Setting Backlight {0} on {1}", level, _ip);
            // HTTP logic to Planar API
            CurrentBacklight = (ushort)level;
            if (OnBacklightChanged != null) OnBacklightChanged();
        }
    }
}
