using System;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Crestron.SimplSharp;

namespace CiscoExternalSource
{
    public delegate void EmptyCallbackHandler();

    public class ExternalSourceDriver
    {
        private HttpClient _client;
        private string _baseUrl;
        private string _authHeader;

        public EmptyCallbackHandler OnSourceChanged { get; set; }

        public string CurrentSourceId { get; private set; }

        public ExternalSourceDriver()
        {
        }

        public static ExternalSourceDriver Create()
        {
            return new ExternalSourceDriver();
        }

        public void Initialize(string ip, string username, string password)
        {
            _baseUrl = string.Format("http://{0}", ip);
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(5);

            var authBytes = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", username, password));
            _authHeader = Convert.ToBase64String(authBytes);
        }

        public void RegisterSource(string id, string name, int connectorId)
        {
            try {
                string xml = string.Format(
                    "<Command><UserInterface><Presentation><ExternalSource><Add>" +
                    "<ConnectorId>{0}</ConnectorId>" +
                    "<Name>{1}</Name>" +
                    "<SourceIdentifier>{2}</SourceIdentifier>" +
                    "<Type>pc</Type>" +
                    "</Add></ExternalSource></Presentation></UserInterface></Command>",
                    connectorId, name, id);

                SendCommand("Register Source", xml).ContinueWith(t => {
                    if (t.IsFaulted) ErrorLog.Error("Cisco RegisterSource Async Error: " + t.Exception.Message);
                });
                SetSourceState(id, "Ready");
                CurrentSourceId = id;
                if (OnSourceChanged != null) OnSourceChanged();
            } catch (Exception ex) {
                ErrorLog.Error("Cisco RegisterSource Error: " + ex.Message);
            }
        }

        public void SetSourceState(string id, string state)
        {
            try {
                string xml = string.Format(
                    "<Command><UserInterface><Presentation><ExternalSource><State><Set>" +
                    "<SourceIdentifier>{0}</SourceIdentifier>" +
                    "<State>{1}</State>" +
                    "</Set></State></ExternalSource></Presentation></UserInterface></Command>",
                    id, state);

                SendCommand("Set Source State", xml).ContinueWith(t => {
                    if (t.IsFaulted) ErrorLog.Error("Cisco SetSourceState Async Error: " + t.Exception.Message);
                });
            } catch (Exception ex) {
                ErrorLog.Error("Cisco SetSourceState Error: " + ex.Message);
            }
        }

        private async Task SendCommand(string label, string xml)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/putxml");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _authHeader);
            request.Content = new StringContent(xml, Encoding.UTF8, "text/xml");

            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                CrestronConsole.PrintLine("CiscoSource: {0} success", label);
            }
            else
            {
                CrestronConsole.PrintLine("CiscoSource: {0} failed: {1}", label, response.StatusCode);
            }
        }
    }
}
