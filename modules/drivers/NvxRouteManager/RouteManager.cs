using System;
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace NvxRouteManager
{
    public delegate void EmptyCallbackHandler();

    public class RouteManager
    {

        public EmptyCallbackHandler OnRouteChanged { get; set; }

        public string CurrentEncoderName { get; private set; }

        public RouteManager()
        {
        }

        public static RouteManager Create()
        {
            return new RouteManager();
        }

        private readonly Dictionary<string, string> _encoders = new Dictionary<string, string>();

        public void AddEncoder(string name, string ip) { _encoders[name] = ip; }

        public void Route(string encoderName, string decoderIp)
        {
            if (_encoders.ContainsKey(encoderName))
            {
                string encoderIp = _encoders[encoderName];
                CrestronConsole.PrintLine("NVX: Routing {0} ({1}) to {2}", encoderName, encoderIp, decoderIp);
                // HTTP logic to NVX Decoder
                CurrentEncoderName = encoderName;
                if (OnRouteChanged != null) OnRouteChanged();
            }
        }
    }
}
