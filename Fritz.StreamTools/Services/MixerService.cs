using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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
		const string TOKEN_FILENAME = "fritz_streamtools_mixer_token.json";

		ClientWebSocket _webSocket;
		IConfiguration _config;
		HttpClient _client;
		IHostingEnvironment _hosting;

		public ILogger _logger { get; }

		OAuthToken _token;

		int _nextCommandId;
		int _channelId;
		int _numberOfFollowers;
		int _numberOfViewers;
		CancellationTokenSource _shutdownRequested;

		public event EventHandler<ServiceUpdatedEventArgs> Updated;

		public int CurrentFollowerCount { get => _numberOfFollowers; }
		public int CurrentViewerCount { get => _numberOfViewers; }

		public string Name { get { return "Mixer"; } }

		public MixerService(IConfiguration config, ILoggerFactory loggerFactory, IHostingEnvironment hosting = null)
		{
			_shutdownRequested = new CancellationTokenSource();
			_config = config;
			_hosting = hosting;
			_client = new HttpClient { BaseAddress = new Uri(API_URL) };
			_client.DefaultRequestHeaders.Add("Accept", "application/json");
			_logger = loggerFactory.CreateLogger("MixerService");
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			_token = LoadToken();
			if (_token == null)
			{
				await DoShortCodeAuth();
			}

			if(_token != null)
			{
				var _ = Task.Run(TokenRefresher);
			}

			//await GetChannelInfo();
			//await Connect(cancellationToken);

			//Logger.LogInformation($"Now monitoring Mixer with {CurrentFollowerCount} followers and {CurrentViewerCount} Viewers");

			//var _ = Task.Factory.StartNew(MixerUpdater);
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
			var segment = new ArraySegment<byte>(new byte[1024]);
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

		#region OAuth

		private async Task<bool> DoShortCodeAuth()
		{
			var client = _client;

			try
			{
				// Send short code request to mixer api
				var data = new
				{
					client_id = _config["StreamServices:Mixer:ClientId"],
					client_secret = _config["StreamServices:Mixer:ClientSecret"],
					scope = ""
				};
				var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
				var result = await client.PostAsync("oauth/shortcode", content);
				var json = await result.Content.ReadAsStringAsync();
				var doc = JObject.Parse(json);

				var code = doc["code"].Value<string>();
				var handle = doc["handle"].Value<string>();
				var expiresIn = doc["expires_in"].Value<int>();
				var expireTime = DateTime.UtcNow + TimeSpan.FromSeconds(expiresIn);

				// Show code to user
				var prevColor = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"Go to 'https://mixer.com/go' and enter code {code} within {expiresIn} seconds");
				Console.ForegroundColor = prevColor;

				// Poll mixer api for user accept, and get auth code
				string authCode = null;
				while(expireTime > DateTime.UtcNow)
				{
					await Task.Delay(500);
					result = await client.GetAsync($"oauth/shortcode/check/{handle}");
					if(result.StatusCode == System.Net.HttpStatusCode.OK)
					{
						json = await result.Content.ReadAsStringAsync();
						doc = JObject.Parse(json);
						authCode = doc["code"].Value<string>();
						break;
					}
				}

				if (string.IsNullOrEmpty(authCode))
				{
					_logger.LogError($"Mixer auth failed: {result.StatusCode}");
					return false;
				}

				// Call mixer token endpoint to login
				var authData = new
				{
					grant_type = "authorization_code",
					client_id = _config["StreamServices:Mixer:ClientId"],
					client_secret = _config["StreamServices:Mixer:ClientSecret"],
					code = authCode
				};
				content = new StringContent(JsonConvert.SerializeObject(authData), Encoding.UTF8, "application/json");
				result = await client.PostAsync("oauth/token", content);
				result.EnsureSuccessStatusCode();
				json = await result.Content.ReadAsStringAsync();
				_token = OAuthToken.Parse(json);
				StoreToken(_token);

				_logger.LogInformation($"Mixer auth succeeded. Access token valid until {_token.ValidUntil} UTC");

				return true;
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Mixer shortcode auth failed");
				return false;
			}
		}

		private async Task TokenRefresher()
		{
			try
			{
				while (true)
				{
					// Refresh token before it expires
					var delay = TimeSpan.FromSeconds((int)((_token.ValidUntil - DateTime.UtcNow).TotalSeconds * .9));
					_logger.LogInformation($"Refreshing mixer access_token in {delay.ToString()}");
					await Task.Delay(delay);

					// Refresh token
					var refreshData = new
					{
						grant_type = "refresh_token",
						refresh_token = _token.RefreshToken,
						client_id = _config["StreamServices:Mixer:ClientId"],
						client_secret = _config["StreamServices:Mixer:ClientSecret"]
					};
					var content = new StringContent(JsonConvert.SerializeObject(refreshData), Encoding.UTF8, "application/json");
					var result = await _client.PostAsync("oauth/token", content);
					result.EnsureSuccessStatusCode();
					var json = await result.Content.ReadAsStringAsync();
					_token = OAuthToken.Parse(json);
					StoreToken(_token);

					_logger.LogInformation($"Mixer access_token refreshed");
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Mixer access_token refreshed failed. You need to restart app to authenticate");
				_token = null;
			}
		}

		private class OAuthToken
		{
			public static OAuthToken Parse(string json)
			{
				var doc = JToken.Parse(json);
				return new OAuthToken
				{
					AccessToken = doc["access_token"].Value<string>(),
					RefreshToken = doc["refresh_token"].Value<string>(),
					ValidUntil = DateTime.UtcNow.AddSeconds(doc["expires_in"].Value<int>())
				};
			}

			public void Update(string newAccessToken, int expiresIn)
			{
				AccessToken = newAccessToken;
				ValidUntil = DateTime.UtcNow.AddSeconds(expiresIn);
			}

			public string AccessToken { get; set; }
			public DateTime ValidUntil { get; set; }
			public string RefreshToken { get; set; }
		}

		private void StoreToken(OAuthToken token)
		{
			var filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), TOKEN_FILENAME);

			try
			{
				var json = JsonConvert.SerializeObject(_token, Formatting.Indented);
				File.WriteAllText(filename, json);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Error saving {0}", filename);
			}
		}

		private OAuthToken LoadToken()
		{
			var filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), TOKEN_FILENAME);
			OAuthToken token = null;

			try
			{
				if (File.Exists(filename))
				{
					var json = File.ReadAllText(filename);
					token = JsonConvert.DeserializeObject<OAuthToken>(json);
					_logger.LogInformation("Loaded token from {0}", filename);
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Error loading {0}", filename);
			}
			return token;
		}

		#endregion
	}
}