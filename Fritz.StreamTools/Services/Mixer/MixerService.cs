using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Helpers;
using Fritz.StreamTools.Services.Mixer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Fritz.StreamTools.Services
{
	public class MixerService : IHostedService, IStreamService, IChatService
	{
		const string API_URL = "https://mixer.com/api/v1/";

		IConfiguration _config;
		HttpClient _client;
		public ILogger _logger;
		IMixerChat _chat;
		IMixerLive _live;
		CancellationTokenSource _shutdownRequested;

		int _channelId;
		int _userId;
		int _numberOfFollowers;
		int _numberOfViewers;

		public event EventHandler<ServiceUpdatedEventArgs> Updated;
		public event EventHandler<ChatMessageEventArgs> OnChatMessage;

		public int CurrentFollowerCount { get => _numberOfFollowers; }
		public int CurrentViewerCount { get => _numberOfViewers; }
		public string Name { get { return "Mixer"; } }

		TimeSpan? _cachedUptime;
		DateTime? _lastUptimeRequest;


		public MixerService(IConfiguration config, ILoggerFactory loggerFactory, IMixerChat chat = null, IMixerLive live = null)
		{
			_shutdownRequested = new CancellationTokenSource();
			_config = config;
			_logger = loggerFactory.CreateLogger("MixerService");

			_client = new HttpClient { BaseAddress = new Uri(API_URL) };
			_client.DefaultRequestHeaders.Add("Accept", "application/json");
			_client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true };

			_live = live ?? new MixerLive(config, loggerFactory, _client, _shutdownRequested.Token);
			_chat = chat ?? new MixerChat(config, loggerFactory, _client, _shutdownRequested.Token);
		}


		#region IHostedService

		/// <summary>
		/// 
		/// </summary>
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var token = _config["StreamServices:Mixer:Token"];
			var authConfigured = !string.IsNullOrEmpty(token);
			if (authConfigured)
			{
				_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			}

			// Get our channel information
			await GetChannelInfo();

			// Connect to live events (viewer/follower count)
			await _live.ConnectAndJoinAsync(_channelId);
			_live.OnLiveEvent += _live_OnLiveEvent;

			if(authConfigured)
			{
				// Connect to chat server
				await _chat.ConnectAndJoinAsync(_userId, _channelId);
				_chat.OnChatMessage += _chat_OnChatMessage;
			}

			_logger.LogInformation($"Now monitoring Mixer with {CurrentFollowerCount} followers and {CurrentViewerCount} Viewers");
		}

		/// <summary>
		/// Closes 
		/// </summary>
		public Task StopAsync(CancellationToken cancellationToken)
		{
			_shutdownRequested.Cancel();
			return Task.CompletedTask;
		}

		#endregion

		/// <summary>
		/// Chat message event handler
		/// </summary>
		private void _chat_OnChatMessage(object sender, ChatMessageEventArgs e)
		{
			e.ServiceName = Name;
			OnChatMessage?.Invoke(this, e);
		}

		/// <summary>
		/// Viewers/followers event handler
		/// </summary>
		private void _live_OnLiveEvent(object sender, EventEventArgs e)
		{
			var data = e.Data;
			ServiceUpdatedEventArgs update = null;

			if (data["numFollowers"] != null && data["numFollowers"].Value<int>() != _numberOfFollowers)
			{
				Interlocked.Exchange(ref _numberOfFollowers, data["numFollowers"].Value<int>());
				_logger.LogTrace($"New Followers on Mixer, new total: {_numberOfFollowers}");

				update = update ?? new ServiceUpdatedEventArgs();
				update.NewFollowers = data["numFollowers"].Value<int>();
			}

			if (data["viewersCurrent"] != null)
			{
				var n = data["viewersCurrent"].Value<int>();
				if (n != Interlocked.Exchange(ref _numberOfViewers, n))
				{
					_logger.LogTrace($"Viewers on Mixer changed, new total: {_numberOfViewers}");
					update = update ?? new ServiceUpdatedEventArgs();
					update.NewViewers = data["viewersCurrent"].Value<int>();
				}
			}

			if(data["online"] != null)
			{
				update = update ?? new ServiceUpdatedEventArgs();
				update.IsOnline = data["online"].Value<bool>();
				_logger.LogTrace($"Online status changed to  {update.IsOnline}");
			}

			if(update != null)
			{
				update.ServiceName = Name;
				Updated?.Invoke(this, update);
			}
		}

		/// <summary>
		/// Get our channel id number and current followers from the api
		/// </summary>
		async Task GetChannelInfo()
		{
			var channel = _config["StreamServices:Mixer:Channel"];
			var response = JObject.Parse(await _client.GetStringAsync($"channels/{WebUtility.UrlEncode(channel)}?fields=id,userId,numFollowers,viewersCurrent"));
			_channelId = response["id"].Value<int>();
			_userId = response["userId"].Value<int>();
			_numberOfFollowers = response["numFollowers"].Value<int>();
			_numberOfViewers = response["viewersCurrent"].Value<int>();
		}

		/// <summary>
		/// Ban the  user
		/// </summary>
		public async Task<bool> BanUserAsync(string userName)
		{
			try
			{
				var userId = await LookupUserId(userName);

				// Add user as banned from our channel
				var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"channels/{_channelId}/users/{userId}")
				{
					Content = new JsonContent(new { add = new[] { "Banned" } })
				};
				var response = await _client.SendAsync(req);
				response.EnsureSuccessStatusCode();
				return true;
			}
			catch (Exception e)
			{
				_logger.LogError("Error unbanning user '{0}': {1}", userName, e.Message);
				return false;
			}
		}

		/// <summary>
		/// Unban the  user
		/// </summary>
		public async Task<bool> UnbanUserAsync(string userName)
		{
			try
			{
				var userId = await LookupUserId(userName);

				// Add user as banned from our channel
				var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"channels/{_channelId}/users/{userId}")
				{
					Content = new JsonContent(new { remove = new[] { "Banned" } })
				};
				var response = await _client.SendAsync(req);
				response.EnsureSuccessStatusCode();
				return true; 
			}
			catch (Exception e)
			{
				_logger.LogError("Error unbanning user '{0}': {1}", userName, e.Message);
				return false;
			}
		}

		/// <summary>
		/// Use the REST API to get id of username
		/// </summary>
		/// <param name="userName">Name of the user</param>
		/// <returns>Id of the user</returns>
		private async Task<int> LookupUserId(string userName)
		{
			var json = await _client.GetStringAsync($"channels/{WebUtility.UrlEncode(userName)}?noCount=1");
			var doc = JToken.Parse(json);
			var userId = (int)doc["id"];
			return userId;
		}

		public Task<bool> SendWhisperAsync(string userName, string message) => _chat.SendWhisperAsync(userName, message);
		public Task<bool> SendMessageAsync(string message) => _chat.SendMessageAsync(message);
		public Task<bool> TimeoutUserAsync(string userName, TimeSpan time) => _chat.TimeoutUserAsync(userName, time);

		/// <summary>
		/// Get stream uptime (cached for 10 seconds)
		/// NOTE: This will hit the REST API when cached value expires
		/// Will be null if stream is offline
		/// </summary>
		public TimeSpan? Uptime
		{
			get
			{
				if (!_lastUptimeRequest.HasValue || DateTime.UtcNow - _lastUptimeRequest.Value > TimeSpan.FromSeconds(10))
				{
					_cachedUptime = GetUptime().Result;
					_lastUptimeRequest = DateTime.UtcNow;
				}
				return _cachedUptime;
			}
		}

		/// <summary>
		/// Get stream uptime from REST API
		/// </summary>
		/// <returns>Uptime, or null if stream is offline</returns>
		private async Task<TimeSpan?> GetUptime()
		{
			var response = await _client.GetAsync($"channels/{_channelId}/manifest.light2");
			if (response.StatusCode != System.Net.HttpStatusCode.OK) return null;
			var json = await response.Content.ReadAsStringAsync();
			var doc = JToken.Parse(json);

			if (doc["startedAt"] != null)
			{
				var now = DateTimeOffset.UtcNow;
				if (DateTimeOffset.TryParse((string)doc["startedAt"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var startedAt))
				{
					if (now > startedAt)
						return TimeSpan.FromSeconds((int)(now - startedAt).TotalSeconds);
				}
			}
			return null;
		}
	}
}
