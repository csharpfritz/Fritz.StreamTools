using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using MixerLib;
using MixerLib.Events;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Helpers;

namespace Fritz.StreamTools.Services
{

	public class MixerService : IHostedService, IStreamService, IChatService, IDisposable
	{
		public static readonly string SERVICE_NAME = "Mixer";

		readonly IConfiguration _config;
		private readonly ILoggerFactory _LoggerFactory;
		readonly ILogger _logger;
		//readonly IMixerConstellation _live;
		readonly CancellationTokenSource _shutdownRequested;
		IMixerClient _Mixer;

		#region Events

		public event EventHandler<StreamLib.Core.ChatMessageEventArgs> ChatMessage;
		public event EventHandler<StreamLib.Core.ChatUserInfoEventArgs> UserJoined;
		public event EventHandler<StreamLib.Core.ChatUserInfoEventArgs> UserLeft;
		public event EventHandler<ServiceUpdatedEventArgs> Updated;

		#endregion

		#region Properties

		public string Name { get => SERVICE_NAME; }
		public int CurrentFollowerCount { get; set; }
		public int CurrentViewerCount { get; set; }
		public bool IsAuthenticated => _Mixer.IsAuthenticated;
		public uint? ChannelID { get => _Mixer.ChannelID; }
		public string ChannelName { get => _Mixer.ChannelName; }
		public uint? UserId { get => _Mixer.UserId; }
		public string UserName { get => _Mixer.UserName; }
		internal string AuthToken { get => _config["StreamServices:Mixer:Token"]; }

		#endregion

		public MixerService(IConfiguration config, ILoggerFactory loggerFactory)
		{
			_shutdownRequested = new CancellationTokenSource();
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			_logger = loggerFactory.CreateLogger(nameof(MixerService));


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

			// Start the Mixer Client
			var authorization = new Auth.ImplicitGrant(AuthToken);
			_Mixer = await MixerClient.StartAsync(channelName, authorization, _LoggerFactory);


			// Get our current channel information
			CurrentFollowerCount = _Mixer.CurrentFollowers;
			CurrentViewerCount = _Mixer.CurrentViewers;

			_logger.LogInformation($"JOINING CHANNEL '{ChannelName}' as {(_Mixer.IsAuthenticated ? UserName : "anonymous(monitor only)")}. with {this.CurrentViewerCount} viewers");

			// Connect to live events (viewer/follower count etc)
			_Mixer.ChannelUpdate += _Mixer_ChannelUpdate;
			_Mixer.ChatMessage += (sender, args) => this.ChatMessage?.Invoke(this, new StreamLib.Core.ChatMessageEventArgs().FromMixerLib(args));
			_Mixer.UserJoined += (sender, args) => this.UserJoined?.Invoke(this, new StreamLib.Core.ChatUserInfoEventArgs().FromMixerLib(args));
			_Mixer.UserLeft += (sender, args) => this.UserLeft?.Invoke(this, new StreamLib.Core.ChatUserInfoEventArgs().FromMixerLib(args));


			_logger.LogInformation($"Now monitoring Mixer with {CurrentFollowerCount} followers and {CurrentViewerCount} Viewers");
		}

		private void _Mixer_ChannelUpdate(object sender, ChannelUpdateEventArgs e)
		{

			CurrentFollowerCount = (int)(e.Channel.NumFollowers.GetValueOrDefault((uint)CurrentFollowerCount));
			CurrentViewerCount = (int)(e.Channel.ViewersCurrent.GetValueOrDefault((uint)CurrentViewerCount));
			this.Updated?.Invoke(this, new ServiceUpdatedEventArgs
			{
				ChannelId = ChannelID.Value,
				IsOnline = true,
				NewFollowers = e.Channel.NumFollowers.HasValue ? (int?)e.Channel.NumFollowers : null,
				NewViewers = e.Channel.ViewersCurrent.HasValue ? (int?)e.Channel.ViewersCurrent : null,
				ServiceName = SERVICE_NAME
			});

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
		/// Get stream uptime (cached for 10 seconds)
		/// NOTE: This will hit the REST API when cached value expires
		/// Will be null if stream is offline
		/// </summary>
		public TimeSpan? Uptime
		{
			get => _Mixer.GetUptime();
		}

		public void Dispose()
		{
			_Mixer.Dispose();
			_shutdownRequested.Dispose();
			GC.SuppressFinalize(this);
		}

		public Task<bool> SendMessageAsync(string message)
			=> _Mixer.SendMessageAsync(message);

		public Task<bool> SendWhisperAsync(string userName, string message)
			=> _Mixer.SendWhisperAsync(userName, message);

		public Task<bool> TimeoutUserAsync(string userName, TimeSpan time)
			=> _Mixer.TimeoutUserAsync(userName, time);

		public Task<bool> BanUserAsync(string userName)
			=> _Mixer.BanUserAsync(userName);

		public Task<bool> UnbanUserAsync(string userName)
			=> _Mixer.UnbanUserAsync(userName);

	}
}
