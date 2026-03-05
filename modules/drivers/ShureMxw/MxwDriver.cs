using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;

namespace ShureMxw
{
    public delegate void EmptyCallbackHandler();

    public class MxwDriver
    {
        private TCPClient _client;
        private string _ip;
        private string _rxBuffer = "";
        private const int Port = 2202;

        public EmptyCallbackHandler OnMuteChanged { get; set; }

        public ushort CurrentMuteIndex { get; private set; }
        public ushort CurrentMuteState { get; private set; }

        public MxwDriver()
        {
        }

        public static MxwDriver Create()
        {
            return new MxwDriver();
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
                    {
                        CrestronConsole.PrintLine("Shure MXW: Connected to " + _ip);
                        _client.ReceiveDataAsync(OnSocketReceive);
                    }
                };
                _client.ConnectToServer();
            }
            catch (Exception ex)
            {
                ErrorLog.Error("Shure MXW: Connection failed: " + ex.Message);
            }
        }

        private void OnSocketReceive(TCPClient client, int count)
        {
            if (count > 0)
            {
                try
                {
                    string msg = Encoding.ASCII.GetString(client.IncomingDataBuffer, 0, count);
                    _rxBuffer += msg;
                    ParseBuffer();
                    
                    if (client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                        client.ReceiveDataAsync(OnSocketReceive);
                }
                catch (Exception ex)
                {
                    ErrorLog.Error("Shure MXW: Rx error: " + ex.Message);
                }
            }
        }

        private void ParseBuffer()
        {
            while (_rxBuffer.Contains(">"))
            {
                int start = _rxBuffer.IndexOf("<");
                int end = _rxBuffer.IndexOf(">");
                if (start >= 0 && end > start)
                {
                    string cmd = _rxBuffer.Substring(start + 1, end - start - 1).Trim();
                    _rxBuffer = _rxBuffer.Substring(end + 1);
                    ParseResponse(cmd);
                }
                else if (end >= 0 && start == -1)
                {
                    _rxBuffer = _rxBuffer.Substring(end + 1);
                }
                else
                {
                    break;
                }
            }
        }

        private void ParseResponse(string cmd)
        {
            // Parse strings like: "REP 1 MUTE ON"
            if (cmd.StartsWith("REP ") && cmd.Contains(" MUTE "))
            {
                string[] parts = cmd.Split(' ');
                if (parts.Length >= 4)
                {
                    if (ushort.TryParse(parts[1], out ushort index))
                    {
                        ushort state = (ushort)(parts[3] == "ON" ? 1 : 0);
                        CurrentMuteIndex = index;
                        CurrentMuteState = state;
                        if (OnMuteChanged != null) OnMuteChanged();
                    }
                }
            }
        }

        public void SetMute(ushort index, ushort muted)
        {
            if (_client != null && _client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                bool isMuted = muted > 0;
                string cmd = string.Format("< SET {0} MUTE {1} >", index, isMuted ? "ON" : "OFF");
                byte[] bytes = Encoding.ASCII.GetBytes(cmd + "\r\n");
                _client.SendData(bytes, (ushort)bytes.Length);
                CrestronConsole.PrintLine("Shure MXW TX: {0}", cmd);
                
                CurrentMuteIndex = index;
                CurrentMuteState = muted;
                if (OnMuteChanged != null) OnMuteChanged();
            }
        }

        public void SetMuteAll(ushort muted)
        {
            for (ushort i = 1; i <= 8; i++)
            {
                SetMute(i, muted);
            }
        }
    }
}
