using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Services.Mixer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// REF: https://dev.mixer.com/reference/constellation/index.html

namespace Fritz.StreamTools.Services
{
	public class MixerService : IHostedService, IStreamService
	{
		const string API_URL = "https://mixer.com/api/v1/";
		const string WS_URL = "wss://constellation.mixer.com";
		const int RECONNECT_DELAY = 10;

		ClientWebSocket _webSocket;
		IConfiguration _config;
		HttpClient _client;
		public ILogger _logger;
		IMixerAuth _auth;
		IMixerChat _chat;
		CancellationTokenSource _shutdownRequested;

		int _nextCommandId;
		int _channelId;
		private int _userId;
		int _numberOfFollowers;
		int _numberOfViewers;

		public event EventHandler<ServiceUpdatedEventArgs> Updated;

		public int CurrentFollowerCount { get => _numberOfFollowers; }
		public int CurrentViewerCount { get => _numberOfViewers; }
		public string Name { get { return "Mixer"; } }

		public MixerService(IConfiguration config, ILoggerFactory loggerFactory, IMixerAuth auth = null, IMixerChat chat = null)
		{
			_shutdownRequested = new CancellationTokenSource();

			_client = new HttpClient { BaseAddress = new Uri(API_URL) };
			_client.DefaultRequestHeaders.Add("Accept", "application/json");

			_auth = auth ?? new MixerAuth(config, loggerFactory, _client, _shutdownRequested.Token);
			if (!string.IsNullOrEmpty(_auth.AccessToken))
			{
				_client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_auth.AccessToken}");
			}

			_chat = chat ?? new MixerChat(config, loggerFactory, _auth, _client, _shutdownRequested.Token);

			_config = config;
			_logger = loggerFactory.CreateLogger("MixerService");
		}

		#region IHostedService

		/// <summary>
		/// 
		/// </summary>
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var authConfigured = !string.IsNullOrEmpty(_config["StreamServices:Mixer:ClientId"]) &&
													 !string.IsNullOrEmpty(_config["StreamServices:Mixer:ClientSecret"]);

			if (_auth.AccessToken == null && authConfigured)
			{
				// Authorize using short code
				await _auth.DoShortCodeAuthAsync();
			}
			if(_auth.AccessToken != null)
			{
				_auth.EnsureTokenRefresherStarted();
			}

			await GetChannelInfo();
			await ConnectEvents(cancellationToken);

			_logger.LogInformation($"Now monitoring Mixer with {CurrentFollowerCount} followers and {CurrentViewerCount} Viewers");

			var _ = Task.Factory.StartNew(MixerUpdater);
			await _chat.ConnectAndJoinAsync(_userId, _channelId);
		}

		/// <summary>
		/// 
		/// </summary>
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

		#endregion

		/// <summary>
		/// Forever try to connect with the remote server
		/// </summary>
		async Task<bool> ConnectEvents(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				_webSocket = new ClientWebSocket();
				_webSocket.Options.SetRequestHeader("x-is-bot", "true");
				if(!string.IsNullOrEmpty(_auth.AccessToken))
				{
					_webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_auth.AccessToken}");
				}

				try
				{
					await _webSocket.ConnectAsync(new Uri(WS_URL), cancellationToken);
					await ReceiveReply(_webSocket);
					await Send(_webSocket, "livesubscribe", $"channel:{_channelId}:update");
					await ReceiveReply(_webSocket);
					return true;
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Event websocket connection to {WS_URL} failed: {e.Message}");
					await Task.Delay(RECONNECT_DELAY * 1000);
				}

			}

			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		async Task<bool> ConnectChat(CancellationToken cancellationToken)
		{
			var url = "wss://chat2-dal.mixer.com";
			var chat = new ClientWebSocket();
			chat.Options.SetRequestHeader("x-is-bot", "true");
			if (!string.IsNullOrEmpty(_auth.AccessToken))
			{
				chat.Options.SetRequestHeader("Authorization", $"Bearer {_auth.AccessToken}");
			}


			try
			{
				await chat.ConnectAsync(new Uri(url), cancellationToken);
				var _ = Task.Run(async () =>
				{
					var segment = new ArraySegment<byte>(new byte[1024]);

					while (!_shutdownRequested.IsCancellationRequested)
					{
						var result = await chat.ReceiveAsync(segment, CancellationToken.None);
						if (result == null || result.Count == 0)
						{
							break;  // Websocket closed
						}

						var json = Encoding.UTF8.GetString(segment.Array.Take(result.Count).ToArray());
						Console.WriteLine(json);


						await Send(chat, "join", $"channel:{_channelId}");
						var reply = await ReceiveReply(chat);
						Console.WriteLine(reply.ToString());
					}
				});

				return true;
			}
			catch(Exception e)
			{
				Debug.WriteLine($"Chat websocket connection to {WS_URL} failed: {e.Message}");

			}
			return false;
		}

		/// <summary>
		/// Get our channel id number and current followers from the api
		/// </summary>
		async Task GetChannelInfo()
		{
			var channel = _config["StreamServices:Mixer:Channel"];
			var response = JObject.Parse(await _client.GetStringAsync($"channels/{channel}?fields=id,userId,numFollowers,viewersCurrent"));
			_channelId = response["id"].Value<int>();
			_userId = response["userId"].Value<int>();
			_numberOfFollowers = response["numFollowers"].Value<int>();
			_numberOfViewers = response["viewersCurrent"].Value<int>();
		}

		/// <summary>
		/// Receive JSON-RPC reply
		/// </summary>
		async Task<JToken> ReceiveReply(ClientWebSocket ws)
		{
			var segment = new ArraySegment<byte>(new byte[1024]);
			var result = await ws.ReceiveAsync(segment, CancellationToken.None);
			var a = segment.Array.Take(result.Count).ToArray();
			var json = Encoding.UTF8.GetString(a);
			return JToken.Parse(json);
		}

		/// <summary>
		/// Send command as JSON-RPC
		/// </summary>
		Task Send(ClientWebSocket ws, string method, string events)
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

			return ws.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
		}

		/// <summary>
		/// 
		/// </summary>
		async Task MixerUpdater()
		{
			var webSocket = _webSocket;
			var segment = new ArraySegment<byte>(new byte[1024]);

			while (!_shutdownRequested.IsCancellationRequested)
			{
				try
				{
					var result = await webSocket.ReceiveAsync(segment, CancellationToken.None);
					if (result == null || result.Count == 0)
					{
						break;  // Websocket closed
					}

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
					if (await ConnectEvents(_shutdownRequested.Token))
					{
						webSocket = _webSocket;
						Debug.WriteLine($"Connection to {WS_URL} re-established");
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		void ParseEvent(JToken data)
		{
			if (data["numFollowers"] != null && data["numFollowers"].Value<int>() != _numberOfFollowers)
			{
				Interlocked.Exchange(ref _numberOfFollowers, data["numFollowers"].Value<int>());
				_logger.LogInformation($"New Followers on Mixer, new total: {_numberOfFollowers}");

				Updated?.Invoke(this, new ServiceUpdatedEventArgs
				{
					ServiceName = Name,
					NewFollowers = data["numFollowers"].Value<int>()
				});
			}

			if (data["viewersCurrent"] != null)
			{
				var n = data["viewersCurrent"].Value<int>();
				if (n != Interlocked.Exchange(ref _numberOfViewers, n))
				{
					Updated?.Invoke(this, new ServiceUpdatedEventArgs
					{
						ServiceName = Name,
						NewViewers = data["viewersCurrent"].Value<int>()
					});
				}
			}
		}
	}
}
