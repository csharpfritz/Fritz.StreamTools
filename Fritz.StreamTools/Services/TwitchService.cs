using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Events.Client;
using TwitchLib.Extensions.Client;
using TwitchLib.Models.API.v5.Streams;
using TwitchLib.Models.Client;
using TwitchLib.Services;

namespace Fritz.StreamTools.Services
{


	public class TwitchService : IHostedService, IStreamService, IChatService
	{

		/// <summary>
		/// Service for connecting and monitoring Twitch
		/// </summary>
		public FollowerService Service { get; private set; }
		private IConfiguration Configuration { get; }
		public ILogger Logger { get; }

		private static int ErrorsReadingViewers = 0;

		public event EventHandler<ServiceUpdatedEventArgs> Updated;
		public event EventHandler<ChatMessageEventArgs> ChatMessage;
		public event EventHandler<ChatUserInfoEventArgs> UserJoined;
		public event EventHandler<ChatUserInfoEventArgs> UserLeft;

		public TwitchService(IConfiguration config, ILoggerFactory loggerFactory)
		{
			this.Configuration = config;
			this.Logger = loggerFactory.CreateLogger("StreamServices");
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			return StartTwitchMonitoring();
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return StopTwitchMonitoring();
		}

		public static int _CurrentFollowerCount;
		public int CurrentFollowerCount
		{
			get { return _CurrentFollowerCount; }
			internal set { _CurrentFollowerCount = value; }
		}

		public static int _CurrentViewerCount;
		private Timer _Timer;
		private TwitchClient _TwitchClient;

		public int CurrentViewerCount { get { return _CurrentViewerCount; } }

		private string ClientId { get { return Configuration["StreamServices:Twitch:ClientId"]; } }

		private string Channel { get { return Configuration["StreamServices:Twitch:Channel"]; } }

		private string ChannelId { get { return Configuration["StreamServices:Twitch:UserId"]; } }

		private string ChatToken {  get {  return Configuration["StreamServices:Twitch:ChatToken"];} }

		public string Name { get { return "Twitch"; } }

		public TimeSpan? Uptime
		{
			get
			{
				var api = new TwitchLib.TwitchAPI(clientId: ClientId, accessToken: ChatToken);
				var v5Stream = CreateTwitchStream(api);
				var myStream = Task.Run(async () => await v5Stream.GetStreamByUserAsync(ChannelId)).GetAwaiter().GetResult();
				return DateTime.UtcNow - myStream.Stream.CreatedAt;

			}
		}

		public bool IsAuthenticated => ChatToken != null;

		private async Task StartTwitchMonitoring()
		{
			var api = new TwitchLib.TwitchAPI(clientId: ClientId, accessToken: ChatToken);
			Service = new FollowerService(api);
			Service.SetChannelByName(Channel);
			await Service.StartService();

			var v5 = new TwitchLib.Channels.V5(api);

			var follows = await v5.GetAllFollowersAsync(ChannelId);
			_CurrentFollowerCount = follows.Count;
			Service.OnNewFollowersDetected += Service_OnNewFollowersDetected;

			var v5Stream = CreateTwitchStream(api);
			if (v5Stream == null) {
				await Task.Delay(2000);
				await StartTwitchMonitoring();
				return;
			}
			var myStream = await v5Stream.GetStreamByUserAsync(ChannelId);
			_CurrentViewerCount = myStream.Stream?.Viewers ?? 0;

			if (ChatToken != null)
			{
				var creds = new ConnectionCredentials(Channel, ChatToken);
				_TwitchClient = new TwitchClient(creds, Channel);
				_TwitchClient.OnUserJoined += _TwitchClient_OnUserJoined;
				_TwitchClient.OnUserLeft += _TwitchClient_OnUserLeft;
				_TwitchClient.OnMessageReceived += _TwitchClient_OnMessageReceived;
				_TwitchClient.OnWhisperReceived += _TwitchClient_OnWhisperReceived;
				_TwitchClient.Connect();
			}


			Logger.LogInformation($"Now monitoring Twitch with {_CurrentFollowerCount} followers and {_CurrentViewerCount} Viewers");

			_Timer = new Timer(CheckViews, v5Stream, 0, 5000);

		}

		private void _TwitchClient_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
		{
			ChatMessage?.Invoke(this, new ChatMessageEventArgs
			{
				IsModerator = false,
				IsOwner = false,
				IsWhisper = true,
				Message = e.WhisperMessage.Message,
				ServiceName = "Twitch",
				UserName = e.WhisperMessage.Username
			});
		}

		private void _TwitchClient_OnUserLeft(object sender, OnUserLeftArgs e)
		{

			UserLeft?.Invoke(this, new ChatUserInfoEventArgs
			{
				ChannelId = 0,
				ServiceName = "Twitch",
				UserId = 0,
				UserName = e.Username
			});

		}

		private void _TwitchClient_OnUserJoined(object sender, OnUserJoinedArgs e)
		{

			UserJoined?.Invoke(this, new ChatUserInfoEventArgs
			{
				ServiceName = "Twitch",
				UserName = e.Username
			});

		}

		private void _TwitchClient_OnMessageReceived(object sender, OnMessageReceivedArgs e)
		{

			ChatMessage?.Invoke(this, new ChatMessageEventArgs
			{
				IsModerator = e.ChatMessage.IsModerator,
				IsOwner = e.ChatMessage.IsBroadcaster,
				IsWhisper = false,
				Message = e.ChatMessage.Message,
				ServiceName = "Twitch",
				UserName = e.ChatMessage.Username
			});

		}

		private async void CheckViews(object state)
		{

			if (!(state is TwitchLib.Streams.V5)) return;

			TwitchLib.Streams.V5 v5Stream = state as TwitchLib.Streams.V5;

			StreamByUser myStream = null;

			try
			{

				myStream = await v5Stream.GetStreamByUserAsync(ChannelId);

			}
			catch (JsonReaderException ex)
			{

				Logger.LogError($"Unable to read stream from Twitch: {ex}");
				return;

			}
			catch (Exception)
			{
				Logger.LogError($"Error while communicating with Twitch");
				return;
			}

			if (_CurrentViewerCount != (myStream.Stream?.Viewers ?? 0))
			{
				_CurrentViewerCount = (myStream.Stream?.Viewers ?? 0);
				Updated?.Invoke(null, new ServiceUpdatedEventArgs
				{
					ServiceName = Name,
					NewViewers = _CurrentViewerCount
				});
			}

		}

		private TwitchLib.Streams.V5 CreateTwitchStream(TwitchLib.TwitchAPI api) {

			TwitchLib.Streams.V5 v5Stream = null;

			try
			{
				v5Stream = new TwitchLib.Streams.V5(api);
				TwitchService.ErrorsReadingViewers = 0;
			}
			catch (Exception ex)
			{
				TwitchService.ErrorsReadingViewers++;
				Logger.LogError(ex, $"Error reading viewers.. {TwitchService.ErrorsReadingViewers} consecutive errors");
			}

			return v5Stream;

		}

		internal void Service_OnNewFollowersDetected(object sender,
		TwitchLib.Events.Services.FollowerService.OnNewFollowersDetectedArgs e)
		{
			Interlocked.Exchange(ref _CurrentFollowerCount, _CurrentFollowerCount + e.NewFollowers.Count);
			Logger.LogInformation($"New Followers on Twitch, new total: {_CurrentFollowerCount}");

			Updated?.Invoke(this, new ServiceUpdatedEventArgs
			{
				ServiceName = Name,
				NewFollowers = _CurrentFollowerCount
			});
		}

		private Task StopTwitchMonitoring()
		{
			Service.StopService();
			return Task.CompletedTask;
		}

		public Task<bool> SendMessageAsync(string message)
		{
			_TwitchClient.SendMessage(message);
			return Task.FromResult(true);
		}

		public Task<bool> SendWhisperAsync(string userName, string message)
		{

			_TwitchClient.SendWhisper(userName, message);
			return Task.FromResult(true);

		}

		public Task<bool> TimeoutUserAsync(string userName, TimeSpan time)
		{

			_TwitchClient.TimeoutUser(userName, time);
			return Task.FromResult(true);


		}

		public Task<bool> BanUserAsync(string userName)
		{
			_TwitchClient.BanUser(userName);
			return Task.FromResult(true);
		}

		public Task<bool> UnbanUserAsync(string userName)
		{
			_TwitchClient.UnbanUser(userName);
			return Task.FromResult(true);
		}
	}

}
