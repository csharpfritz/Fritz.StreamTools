﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Services.Mixer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fritz.StreamTools.Services
{
	public class MixerService : IHostedService, IStreamService, IChatService, IDisposable
	{
		readonly IConfiguration _config;
		readonly ILogger _logger;
		readonly IMixerChat _chat;
		readonly IMixerConstellation _live;
		readonly CancellationTokenSource _shutdownRequested;
		readonly IMixerRestClient _restClient;

		int _numberOfFollowers;
		int _numberOfViewers;

		public event EventHandler<ServiceUpdatedEventArgs> Updated;
		public event EventHandler<ChatMessageEventArgs> ChatMessage;
		public event EventHandler<ChatUserInfoEventArgs> UserJoined;
		public event EventHandler<ChatUserInfoEventArgs> UserLeft;

		public int CurrentFollowerCount { get => _numberOfFollowers; }
		public int CurrentViewerCount { get => _numberOfViewers; }
		public string Name { get { return "Mixer"; } }
		public bool IsAuthenticated => ( _chat?.IsAuthenticated ).GetValueOrDefault();

		private bool? _isOnline;
		DateTimeOffset? _streamStartedAt;

		public MixerService(IConfiguration config, ILoggerFactory loggerFactory, IMixerFactory factory = null)
		{
			if (loggerFactory == null)
				throw new ArgumentNullException(nameof(loggerFactory));

			_shutdownRequested = new CancellationTokenSource();
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_logger = loggerFactory.CreateLogger(nameof(MixerService));

			factory = factory ?? new MixerFactory(config, loggerFactory);

			_restClient = factory.CreateRestClient();
			_live = factory.CreateConstellation(_shutdownRequested.Token);
			_chat = factory.CreateChat(_restClient, _shutdownRequested.Token);
		}

		#region IHostedService

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var channelName = _config["StreamServices:Mixer:Channel"];
			if (string.IsNullOrWhiteSpace(channelName))
			{
				// Bail out early if no channel are given in configuration
				_logger.LogWarning("No mixer channel set in configuration (StreamServices:Mixer:Channel)");
				return;
			}

			// Get our current channel information
			var (viewers, followers) = await _restClient.InitAsync(_config["StreamServices:Mixer:Channel"], _config["StreamServices:Mixer:Token"]);
			_numberOfFollowers = followers;
			_numberOfViewers = viewers;

			_logger.LogInformation("JOINING CHANNEL '{0}' as {0}", _restClient.ChannelName, _restClient.HasToken ? _restClient.UserName : "anonymous (monitor only)");

			// Connect to live events (viewer/follower count)
			await _live.ConnectAndJoinAsync(_restClient.ChannelId.Value);
			_live.ConstellationEvent += _live_LiveEvent;

			// Connect to chat server
			await _chat.ConnectAndJoinAsync(_restClient.UserId.GetValueOrDefault(), _restClient.ChannelId.Value);
			_chat.ChatMessage += _chat_ChatMessage;
			_chat.UserJoined += _chat_UserJoined;
			_chat.UserLeft += _chat_UserLeft;

			_logger.LogInformation($"Now monitoring Mixer with {CurrentFollowerCount} followers and {CurrentViewerCount} Viewers");
		}

		private void _chat_UserJoined(object sender, ChatUserInfoEventArgs e)
		{
			e.ServiceName = Name;
			UserJoined?.Invoke(this, e);
		}

		private void _chat_UserLeft(object sender, ChatUserInfoEventArgs e)
		{
			e.ServiceName = Name;
			UserLeft?.Invoke(this, e);
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
		private void _chat_ChatMessage(object sender, ChatMessageEventArgs e)
		{
			e.ServiceName = Name;
			ChatMessage?.Invoke(this, e);
		}

		/// <summary>
		/// Viewers/followers/IsOnline event handler
		/// </summary>
		private void _live_LiveEvent(object sender, ConstellationEventArgs e)
		{
			// Maybe check e.ChannelId == our channelId ???

			switch (e.Event)
			{
				case "update":
					HandleUpdate(e.Payload.GetObject< WS.LivePayload>());
					break;
				case "followed":
					HandleFollowed(e.Payload.GetObject<WS.FollowedPayload>());
					break;
				case "subscribed":
					HandleSubscribed(e.Payload.GetObject<WS.SubscribedPayload>());
					break;
				case "resubscribed":
				case "resubShared":
					HandleResubscribed(e.Payload.GetObject<WS.ResubscribedPayload>());
					break;
				case "hosted":
					HandleHosted(e.Payload.GetObject<WS.HostedPayload>());
					break;
				case "unhosted":
					HandleUnhosted(e.Payload.GetObject<WS.HostedPayload>());
					break;
			}
		}

		private ServiceUpdatedEventArgs HandleUpdate(WS.LivePayload data)
		{
			ServiceUpdatedEventArgs update = null;

			if (data.NumFollowers.HasValue && data.NumFollowers != _numberOfFollowers)
			{
				_numberOfFollowers = (int)data.NumFollowers.Value;
				update = update ?? new ServiceUpdatedEventArgs();
				update.NewFollowers = _numberOfFollowers;
				_logger.LogTrace($"New Followers on Mixer, new total: {_numberOfFollowers}");
			}

			if (data.ViewersCurrent.HasValue)
			{
				var count = (int)data.ViewersCurrent.Value;
				if (count != _numberOfViewers)
				{
					_numberOfViewers = count;
					update = update ?? new ServiceUpdatedEventArgs();
					update.NewViewers = count;
					_logger.LogTrace($"Viewers on Mixer changed, new total: {count}");
				}
			}

			if (data.Online.HasValue)
			{
				update = update ?? new ServiceUpdatedEventArgs();
				update.IsOnline = _isOnline = data.Online.Value;
				_streamStartedAt = null;  // Clear cached stream start time
				_logger.LogTrace($"Online status changed to  {update.IsOnline}");
			}

			if (update != null)
			{
				update.ServiceName = Name;
				Updated?.Invoke(this, update);
			}

			return update;
		}

		void HandleFollowed(WS.FollowedPayload payload)
		{
			_logger.LogInformation("{0} {1}", payload.User.Username, payload.Following ? "followed" : "unfollowed");
		}

		void HandleHosted(WS.HostedPayload payload)
		{
			_logger.LogInformation("{0} started hosting for {1} viewers", payload.Hoster.Name, payload.Hoster.ViewersCurrent);
		}

		void HandleUnhosted(WS.HostedPayload payload)
		{
			_logger.LogInformation("{0} stopped hosting", payload.Hoster.Name);
		}

		void HandleSubscribed(WS.SubscribedPayload payload)
		{
			_logger.LogInformation("{0} subscribed", payload.User.Username);
		}

		void HandleResubscribed(WS.ResubscribedPayload payload)
		{
			_logger.LogInformation("{0} re-subscribed since {1} for {1} month", payload.User.Username, payload.Since, payload.TotalMonths);
		}

		public Task<bool> BanUserAsync(string userName) => _restClient.BanUserAsync(userName);
		public Task<bool> UnbanUserAsync(string userName) => _restClient.UnbanUserAsync(userName);
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
				if (_isOnline == false) return null;
				if (!_streamStartedAt.HasValue)
					_streamStartedAt = _restClient.GetStreamStartedAtAsync().Result;
				if (!_streamStartedAt.HasValue) return null;

				// Remove milliseconds
				var seconds = ( DateTime.UtcNow - _streamStartedAt.Value ).Ticks / TimeSpan.TicksPerSecond;
				return TimeSpan.FromSeconds(Math.Max(0, seconds));
			}
		}

		public void Dispose()
		{
			_chat.Dispose();
			_live.Dispose();
			_restClient.Dispose();
			_shutdownRequested.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
