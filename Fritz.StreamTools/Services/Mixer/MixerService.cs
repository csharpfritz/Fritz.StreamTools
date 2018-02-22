using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
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
		IMixerAuth _auth;
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

		public MixerService(IConfiguration config, ILoggerFactory loggerFactory, IMixerAuth auth = null, IMixerChat chat = null, IMixerLive live = null)
		{
			_shutdownRequested = new CancellationTokenSource();
			_config = config;
			_logger = loggerFactory.CreateLogger("MixerService");

			_client = new HttpClient { BaseAddress = new Uri(API_URL) };
			_client.DefaultRequestHeaders.Add("Accept", "application/json");

			_auth = auth ?? new MixerAuth(config, loggerFactory, _client);
			_live = live ?? new MixerLive(config, loggerFactory, _auth, _client, _shutdownRequested.Token);
			_chat = chat ?? new MixerChat(config, loggerFactory, _auth, _client, _shutdownRequested.Token);
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
				await _auth.RefreshTokenIfNeeded();
				_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
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

			if (data["numFollowers"] != null && data["numFollowers"].Value<int>() != _numberOfFollowers)
			{
				Interlocked.Exchange(ref _numberOfFollowers, data["numFollowers"].Value<int>());
				_logger.LogTrace($"New Followers on Mixer, new total: {_numberOfFollowers}");

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
					_logger.LogTrace($"Viewers on Mixer changed, new total: {_numberOfViewers}");
					Updated?.Invoke(this, new ServiceUpdatedEventArgs
					{
						ServiceName = Name,
						NewViewers = data["viewersCurrent"].Value<int>()
					});
				}
			}
		}

		/// <summary>
		/// Get our channel id number and current followers from the api
		/// </summary>
		async Task GetChannelInfo()
		{
			await _auth.RefreshTokenIfNeeded();

			var channel = _config["StreamServices:Mixer:Channel"];
			var response = JObject.Parse(await _client.GetStringAsync($"channels/{channel}?fields=id,userId,numFollowers,viewersCurrent"));
			_channelId = response["id"].Value<int>();
			_userId = response["userId"].Value<int>();
			_numberOfFollowers = response["numFollowers"].Value<int>();
			_numberOfViewers = response["viewersCurrent"].Value<int>();
		}

		public Task<bool> SendWhisperAsync(string userName, string message) => _chat.SendWhisperAsync(userName, message);
		public Task<bool> BanUserAsync(string userName) => _chat.BanUserAsync(userName);
		public Task<bool> SendMessageAsync(string message) => _chat.SendMessageAsync(message);
		public Task<bool> TimeoutUserAsync(string userName, TimeSpan time) => _chat.TimeoutUserAsync(userName, time);
	}
}
