using System;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Helpers;
using Fritz.StreamTools.Services.Mixer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fritz.StreamTools.Services
{
	public interface IMixerService : IChatService, IStreamService
	{
		uint? ChannelID { get; }
		string ChannnelName { get; }
		uint? UserId { get; }
		string UserName { get; }

		event EventHandler<FollowedEventArgs> Followed;
		event EventHandler<HostedEventArgs> Hosted;
		event EventHandler<SubscribedEventArgs> Subscribed;
		event EventHandler<ResubscribedEventArgs> Resubscribed;
	}

	public class MixerService : IHostedService, IMixerService, IDisposable
	{
		public static readonly string SERVICE_NAME = "Mixer";

		readonly IConfiguration _config;
		readonly ILogger _logger;
		readonly IMixerChat _chat;
		readonly IMixerConstellation _live;
		readonly CancellationTokenSource _shutdownRequested;
		readonly IMixerRestClient _restClient;

		public event EventHandler<ServiceUpdatedEventArgs> Updated;
		public event EventHandler<ChatMessageEventArgs> ChatMessage;
		public event EventHandler<ChatUserInfoEventArgs> UserJoined;
		public event EventHandler<ChatUserInfoEventArgs> UserLeft;

		public event EventHandler<FollowedEventArgs> Followed;
		public event EventHandler<HostedEventArgs> Hosted;
		public event EventHandler<SubscribedEventArgs> Subscribed;
		public event EventHandler<ResubscribedEventArgs> Resubscribed;

		public string Name { get => SERVICE_NAME; }
		public int CurrentFollowerCount { get => _liveParser.Followers; }
		public int CurrentViewerCount { get => _liveParser.Viewers; }
		public bool IsAuthenticated => ( _chat?.IsAuthenticated ).GetValueOrDefault();
		public uint? ChannelID { get => _restClient.ChannelId; }
		public string ChannnelName { get => _restClient.ChannelName; }
		public uint? UserId { get => _restClient.UserId; }
		public string UserName { get => _restClient.UserName; }

		readonly ConstellationEventParser _liveParser;
		readonly ChatEventParser _chatParser;

		public MixerService(IConfiguration config, ILoggerFactory loggerFactory, IMixerFactory factory = null)
		{
			if (loggerFactory == null)
				throw new ArgumentNullException(nameof(loggerFactory));

			_shutdownRequested = new CancellationTokenSource();
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_logger = loggerFactory.CreateLogger(nameof(MixerService));

			factory = factory ?? new MixerFactory(config, loggerFactory);

			_liveParser = new ConstellationEventParser(_logger, FireEvent);
			_chatParser = new ChatEventParser(_logger, FireEvent);

			_restClient = factory.CreateRestClient();
			_live = factory.CreateConstellation(_liveParser, _shutdownRequested.Token);
			_chat = factory.CreateChat(_restClient, _chatParser, _shutdownRequested.Token);
		}

		// Can be used during future testing
		internal MixerService(IConfiguration config, ILoggerFactory loggerFactory, IMixerFactory factory,
												ConstellationEventParser liveParser = null, ChatEventParser chatParser = null)
			: this(config, loggerFactory, factory)
		{
			_liveParser = liveParser ?? _liveParser;
			_chatParser = chatParser ?? _chatParser;
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
			var (online, viewers, followers) = await _restClient.InitAsync(channelName, _config["StreamServices:Mixer:Token"]);
			_liveParser.IsOnline = online;
			_liveParser.Followers = followers;
			_liveParser.Viewers = viewers;

			_logger.LogInformation("JOINING CHANNEL '{0}' as {1}. {2} with {3} viewers", _restClient.ChannelName,
				_restClient.HasToken ? _restClient.UserName : "anonymous (monitor only)",
				_liveParser.IsOnline == true ? "ONLINE" : "OFFLINE",
				_liveParser.Viewers);

			// Connect to live events (viewer/follower count etc)
			await _live.ConnectAndJoinAsync(_restClient.ChannelId.Value);

			// Connect to chat server
			await _chat.ConnectAndJoinAsync(_restClient.UserId.GetValueOrDefault(), _restClient.ChannelId.Value);

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

		private void FireEvent(string name, EventArgs args) => ReflectionHelper.RaiseEvent(this, name, args);

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
			get {
				if (_liveParser.IsOnline == false)
					return null;
				if (!_liveParser.StreamStartedAt.HasValue)
					_liveParser.StreamStartedAt = _restClient.GetStreamStartedAtAsync().Result;
				if (!_liveParser.StreamStartedAt.HasValue)
					return null;

				// Remove milliseconds
				var seconds = ( DateTime.UtcNow - _liveParser.StreamStartedAt.Value ).Ticks / TimeSpan.TicksPerSecond;
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
