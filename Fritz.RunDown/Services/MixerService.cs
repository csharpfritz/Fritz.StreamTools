using System;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// REF: https://dev.mixer.com/reference/constellation/index.html

namespace Fritz.RunDown.Services
{
    public class MixerService : IHostedService
    {
        const string API_URL = "https://mixer.com/api/v1/";
        const string WS_URL = "wss://constellation.mixer.com";

        private ClientWebSocket _ws;
        private IConfiguration _config;
        private HttpClient _client;
        private int _nextCommandId = 0;
        private int _channelId;
        private int _numberOfFollowers;
        private int _numberOfViewers;

        public event EventHandler Updated;

        public int CurrentFollowerCount { get => _numberOfFollowers; }
        public int CurrentViewerCount { get => _numberOfViewers; }

        public MixerService(IConfiguration config)
        {
            _config = config;
            _client = new HttpClient { BaseAddress = new Uri(API_URL) };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await GetChannelInfo();

            _ws = new ClientWebSocket();
            _ws.Options.SetRequestHeader("x-is-bot", "true");
            await _ws.ConnectAsync(new Uri(WS_URL), cancellationToken);
            await ReceiveReply();
            await Send("livesubscribe", $"channel:{_channelId}:update");
            await ReceiveReply();
            var _ = Task.Factory.StartNew(MixerUpdater);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_ws != null)
            {
                _ws.Dispose();
                _ws = null;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Get our channel id number and current followers from the api
        /// </summary>
        private async Task GetChannelInfo()
        {
            string channel = _config["StreamServices:Mixer:Channel"];
            var response = JObject.Parse(await _client.GetStringAsync($"channels/{channel}?fields=id,numFollowers,viewersCurrent"));
            _channelId = response["id"].Value<int>();
            _numberOfFollowers = response["numFollowers"].Value<int>();
            _numberOfViewers = response["viewersCurrent"].Value<int>();
        }

        /// <summary>
        /// Receive JSON-RPC reply
        /// </summary>
        private async Task<JToken> ReceiveReply()
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(new byte[1024]);
            var result = await _ws.ReceiveAsync(segment, CancellationToken.None);
            var a = segment.Array.Take(result.Count).ToArray();
            var json = Encoding.UTF8.GetString(a);
            return JToken.Parse(json);
        }

        /// <summary>
        /// Send command as JSON-RPC
        /// </summary>
        private Task Send(string method, string events)
        {
            var data = new
            {
                id = _nextCommandId++,
                type = "method",
                method,
                @params = new
                {
                    events = new string[]
                    {
                        events
                    }

                }
            };

            var json = JsonConvert.SerializeObject(data);
            var buf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));

            return _ws.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task MixerUpdater()
        {
            var ws = _ws;
            ArraySegment<byte> segment = new ArraySegment<byte>(new byte[1024]);

            try
            {
                while (true)
                {
                    var result = await ws.ReceiveAsync(segment, CancellationToken.None);
                    var json = Encoding.UTF8.GetString(segment.Array.Take(result.Count).ToArray());
                    var doc = JObject.Parse(json);
                    if (doc["type"] != null && doc["type"].Value<string>() == "event")
                    {
                        ParseEvent(doc["data"]["payload"]);
                    }
                }

            }
            catch (ObjectDisposedException)
            {
                // NOP
            }
        }

        private void ParseEvent(JToken data)
        {
            if (data["numFollowers"] != null)
            {
                Interlocked.Exchange(ref _numberOfFollowers, data["numFollowers"].Value<int>());
                Updated?.Invoke(this, EventArgs.Empty);
            }
            if (data["viewersCurrent"] != null)
            {
                var n = data["viewersCurrent"].Value<int>();
                if (n != Interlocked.Exchange(ref _numberOfViewers, n))
                {
                    Updated?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

}
