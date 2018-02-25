using System;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Services.Mixer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Fritz.StreamTools.Services
{
	public class MixerService : IHostedService, IStreamService, IChatService, IDisposable
	{
		readonly IConfiguration _config;
		readonly ILogger _logger;
		readonly IMixerChat _chat;
		readonly IMixerLive _live;
		readonly CancellationTokenSource _shutdownRequested;
		readonly IMixerRestClient _restClient;

		int _channelId;
		int _userId;
		int _numberOfFollowers;
		int _numberOfViewers;

		public event EventHandler<ServiceUpdatedEventArgs> Updated;
		public event EventHandler<ChatMessageEventArgs> ChatMessage;

		public int CurrentFollowerCount { get => _numberOfFollowers; }
		public int CurrentViewerCount { get => _numberOfViewers; }
		public string Name { get { return "Mixer"; } }

		private bool? _isOnline;
		DateTimeOffset? _streamStartedAt;


		public MixerService(IConfiguration config, ILoggerFactory loggerFactory, IMixerFactory factory = null)
		{
			if (loggerFactory == null)
				throw new ArgumentNullException(nameof(loggerFactory));

			_shutdownRequested = new CancellationTokenSource();
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_logger = loggerFactory.CreateLogger("MixerService");

			factory = factory ?? new MixerFactory(config, loggerFactory);

			_restClient = factory.CreateMixerRestClient(_config["StreamServices:Mixer:Channel"], _config["StreamServices:Mixer:Token"]);

			_live = factory.CreateMixerLive(_shutdownRequested.Token);
			_chat = factory.CreateMixerChat(_restClient, _shutdownRequested.Token);
		}


		#region IHostedService

		/// <summary>
		/// 
		/// </summary>
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			// Get our current channel information
			var info = await _restClient.GetChannelInfoAsync();
			_channelId = info.Id;
			_userId = info.UserId;
			_numberOfFollowers = info.NumberOfFollowers;
			_numberOfViewers = info.NumberOfViewers;

			// Connect to live events (viewer/follower count)
			await _live.ConnectAndJoinAsync(_channelId);
			_live.LiveEvent += _live_LiveEvent;

			if(_restClient.HasToken)
			{
				// Connect to chat server
				await _chat.ConnectAndJoinAsync(_userId, _channelId);
				_chat.ChatMessage += _chat_ChatMessage;
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
		private void _chat_ChatMessage(object sender, ChatMessageEventArgs e)
		{
			e.ServiceName = Name;
			ChatMessage?.Invoke(this, e);
		}

		/// <summary>
		/// Viewers/followers event handler
		/// </summary>
		private void _live_LiveEvent(object sender, EventEventArgs e)
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
				_isOnline = update.IsOnline.Value;
				_streamStartedAt = null;	// Clear cached stream start time
				_logger.LogTrace($"Online status changed to  {update.IsOnline}");
			}

			if(update != null)
			{
				update.ServiceName = Name;
				Updated?.Invoke(this, update);
			}
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
				if (_isOnline.HasValue && !_isOnline.Value) return null;
				if (!_streamStartedAt.HasValue)
				{
					_streamStartedAt = _restClient.GetStreamStartedAtAsync().Result;
				}
				var startedAt = _streamStartedAt;
				if (!startedAt.HasValue) return null;

				// Remove milliseconds
				var t = DateTimeOffset.UtcNow - startedAt.Value;
				return TimeSpan.FromSeconds(Math.Round(t.TotalSeconds));
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
