using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;

namespace ShureMxa920
{
    public delegate void EmptyCallbackHandler();

    public class Mxa920Driver
    {
        private TCPClient _client;
        private string _ip;
        private const int Port = 2202; // Shure Control Port

        public EmptyCallbackHandler OnMuteChanged { get; set; }
        
        public ushort CurrentMuteState { get; private set; }

        public Mxa920Driver()
        {
        }

        public static Mxa920Driver Create()
        {
            return new Mxa920Driver();
        }

        public void Initialize(string ip)
        {
            _ip = ip;
        }
    
        public void Connect()
        {
            try
            {
                _client = new TCPClient(_ip, Port, 1024);
                _client.SocketStatusChange += (c, st) => {
                    if (st == SocketStatus.SOCKET_STATUS_CONNECTED)
                        CrestronConsole.PrintLine("Shure MXA920: Connected to " + _ip);
                };
                _client.ConnectToServer();
            }
            catch (Exception ex)
            {
                ErrorLog.Error("Shure MXA920: Connection failed: " + ex.Message);
            }
        }

        public void SetMute(ushort muted)
        {
            bool isMuted = muted > 0;
            if (_client != null && _client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                // MXA920 Device Mute Command
                string cmd = string.Format("< SET DEVICE_AUDIO_MUTE {0} >", isMuted ? "ON" : "OFF");
                byte[] bytes = Encoding.ASCII.GetBytes(cmd + "\r\n");
                _client.SendData(bytes, (ushort)bytes.Length);
                CrestronConsole.PrintLine("Shure MXA920 TX: {0}", cmd);
                
                CurrentMuteState = muted;
                if (OnMuteChanged != null) OnMuteChanged();
            }
        }
    }
}
