using System;
using System.Diagnostics;
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

namespace Fritz.StreamTools.Services
{
    public class MixerService : IHostedService
    {
        const string API_URL = "https://mixer.com/api/v1/";
        const string WS_URL = "wss://constellation.mixer.com";
        const int RECONNECT_DELAY = 10;

        ClientWebSocket _webSocket;
        IConfiguration _config;
        HttpClient _client;
        int _nextCommandId;
        int _channelId;
        int _numberOfFollowers;
        int _numberOfViewers;
        CancellationTokenSource _shutdownRequested;

        public event EventHandler Updated;

        public int CurrentFollowerCount { get => _numberOfFollowers; }
        public int CurrentViewerCount { get => _numberOfViewers; }

        public MixerService(IConfiguration config)
        {
            _shutdownRequested = new CancellationTokenSource();
            _config = config;
            _client = new HttpClient { BaseAddress = new Uri(API_URL) };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await GetChannelInfo();
            await Connect(cancellationToken);
            var _ = Task.Factory.StartNew(MixerUpdater);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _shutdownRequested.Cancel();

            if (_webSocket != null)
            {
                _webSocket.Dispose();
                _webSocket = null;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Forever try to connect with the remote server
        /// </summary>
        async Task<bool> Connect(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _webSocket = new ClientWebSocket();
                _webSocket.Options.SetRequestHeader("x-is-bot", "true");

                try
                {
                    await _webSocket.ConnectAsync(new Uri(WS_URL), cancellationToken);
                    await ReceiveReply();
                    await Send("livesubscribe", $"channel:{_channelId}:update");
                    await ReceiveReply();
                    return true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Websocket connection to {WS_URL} failed: {e.Message}");
                    await Task.Delay(RECONNECT_DELAY * 1000);
                }

            }
            return false;
        }

        /// <summary>
        /// Get our channel id number and current followers from the api
        /// </summary>
        async Task GetChannelInfo()
        {
            var channel = _config["StreamServices:Mixer:Channel"];
            var response = JObject.Parse(await _client.GetStringAsync($"channels/{channel}?fields=id,numFollowers,viewersCurrent"));
            _channelId = response["id"].Value<int>();
            _numberOfFollowers = response["numFollowers"].Value<int>();
            _numberOfViewers = response["viewersCurrent"].Value<int>();
        }

        /// <summary>
        /// Receive JSON-RPC reply
        /// </summary>
        async Task<JToken> ReceiveReply()
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(new byte[1024]);
            var result = await _webSocket.ReceiveAsync(segment, CancellationToken.None);
            var a = segment.Array.Take(result.Count).ToArray();
            var json = Encoding.UTF8.GetString(a);
            return JToken.Parse(json);
        }

        /// <summary>
        /// Send command as JSON-RPC
        /// </summary>
        Task Send(string method, string events)
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

            return _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        async Task MixerUpdater()
        {
            var webSocket = _webSocket;
            ArraySegment<byte> segment = new ArraySegment<byte>(new byte[1024]);

            while (!_shutdownRequested.IsCancellationRequested)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(segment, CancellationToken.None);
                    if (result == null || result.Count == 0)
                        break;  // Websocket closed

                    var json = Encoding.UTF8.GetString(segment.Array.Take(result.Count).ToArray());
                    var doc = JObject.Parse(json);
                    if (doc["type"] != null && doc["type"].Value<string>() == "event")
                    {
                        ParseEvent(doc["data"]["payload"]);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Normal websocket close. Break out of forever loop
                    break;
                }
                catch (Exception)
                {
                    // Connection was proberly terminated abnormally
                    Debug.WriteLine($"Lost connection to {WS_URL}. Will try to reconnect");

                    _webSocket.Dispose();
                    _webSocket = null;

                    // Re-connect
                    if (await Connect(_shutdownRequested.Token))
                    {
                        webSocket = _webSocket;
                        Debug.WriteLine($"Connection to {WS_URL} re-established");
                    }
                }
            }
        }

        void ParseEvent(JToken data)
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
