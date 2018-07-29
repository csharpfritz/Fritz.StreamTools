using Fritz.StreamLib.Core;
using Fritz.Twitch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Services
{


	public class TwitchService : IHostedService, IStreamService, IChatService
	{

		private IConfiguration Configuration { get; }
		public ILogger Logger { get; }

		private readonly Proxy Proxy;

		private ChatClient _ChatClient;

		public event EventHandler<ServiceUpdatedEventArgs> Updated;
		public event EventHandler<ChatMessageEventArgs> ChatMessage;
		public event EventHandler<ChatUserInfoEventArgs> UserJoined;
		public event EventHandler<ChatUserInfoEventArgs> UserLeft {
			add { }
			remove { }
		}

		public TwitchService(IConfiguration config, ILoggerFactory loggerFactory, Fritz.Twitch.Proxy proxy, Fritz.Twitch.ChatClient chatClient)
		{
			this.Configuration = config;
			this.Logger = loggerFactory.CreateLogger("StreamServices");
			this.Proxy = proxy;
			this._ChatClient = chatClient;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await StartTwitchMonitoring();
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

		public int CurrentViewerCount { get { return _CurrentViewerCount; } }

		public string Name { get { return "Twitch"; } }

		public ValueTask<TimeSpan?> Uptime() => Proxy.Uptime();

		public bool IsAuthenticated => true;

		private async Task StartTwitchMonitoring()
		{
			try
			{
				_ChatClient.Connected += (c, args) => Logger.LogInformation("Now connected to Twitch Chat");
				_ChatClient.NewMessage += _ChatClient_NewMessage;
				_ChatClient.UserJoined += _ChatClient_UserJoined;
				_ChatClient.Init();

				_CurrentFollowerCount = await Proxy.GetFollowerCountAsync();
				Proxy.NewFollowers += Proxy_NewFollowers;
				Proxy.WatchFollowers(10000);

				_CurrentViewerCount = await Proxy.GetViewerCountAsync();
				Proxy.NewViewers += Proxy_NewViewers;
				Proxy.WatchViewers();

				Logger.LogInformation($"Now monitoring Twitch with {_CurrentFollowerCount} followers and {_CurrentViewerCount} Viewers");
			}
			catch (Exception ex)
			{
				Logger.LogWarning("StartTwitchMonitoring failed: " + ex.Message);
			}
		}

		private void _ChatClient_UserJoined(object sender, ChatUserJoinedEventArgs e)
		{
			UserJoined?.Invoke(this, new ChatUserInfoEventArgs
			{
				ServiceName = "Twitch",
				UserName = e.UserName
			});
		}

		private void _ChatClient_NewMessage(object sender, NewMessageEventArgs e)
		{

			ChatMessage?.Invoke(this, new ChatMessageEventArgs
			{
				IsModerator = false,
				IsOwner = (_ChatClient.ChannelName == e.UserName),
				IsWhisper = e.IsWhisper,
				Message = e.Message,
				ServiceName = "Twitch",
				UserName = e.UserName
			});
		}

		private void Proxy_NewViewers(object sender, NewViewersEventArgs e)
		{
			Interlocked.Exchange(ref _CurrentViewerCount, e.ViewerCount);
			Logger.LogInformation($"New Viewers on Twitch, new total: {_CurrentViewerCount}");

			Updated?.Invoke(this, new ServiceUpdatedEventArgs
			{
				ServiceName = Name,
				NewViewers = _CurrentViewerCount
			});
		}

		private void Proxy_NewFollowers(object sender, NewFollowersEventArgs e)
		{
			Interlocked.Exchange(ref _CurrentFollowerCount, e.FollowerCount);
			Logger.LogInformation($"New Followers on Twitch, new total: {_CurrentFollowerCount}");

			Updated?.Invoke(this, new ServiceUpdatedEventArgs
			{
				ServiceName = Name,
				NewFollowers = _CurrentFollowerCount
			});
		}

		//private void _TwitchClient_OnConnected(object sender, OnConnectedArgs e)
		//{
		//	Logger.LogInformation("Now connected to Twitch Chat Room");
		//}

		//private void _TwitchClient_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
		//{
		//	ChatMessage?.Invoke(this, new ChatMessageEventArgs
		//	{
		//		IsModerator = false,
		//		IsOwner = false,
		//		IsWhisper = true,
		//		Message = e.WhisperMessage.Message,
		//		ServiceName = "Twitch",
		//		UserName = e.WhisperMessage.Username
		//	});
		//}

		//private void _TwitchClient_OnUserLeft(object sender, OnUserLeftArgs e)
		//{

		//	UserLeft?.Invoke(this, new ChatUserInfoEventArgs
		//	{
		//		ChannelId = 0,
		//		ServiceName = "Twitch",
		//		UserId = 0,
		//		UserName = e.Username
		//	});

		//}

		//private void _TwitchClient_OnUserJoined(object sender, OnUserJoinedArgs e)
		//{

		//	UserJoined?.Invoke(this, new ChatUserInfoEventArgs
		//	{
		//		ServiceName = "Twitch",
		//		UserName = e.Username
		//	});

		//}

		//private void _TwitchClient_OnMessageReceived(object sender, OnMessageReceivedArgs e)
		//{

		//	ChatMessage?.Invoke(this, new ChatMessageEventArgs
		//	{
		//		IsModerator = e.ChatMessage.IsModerator,
		//		IsOwner = e.ChatMessage.IsBroadcaster,
		//		IsWhisper = false,
		//		Message = e.ChatMessage.Message,
		//		ServiceName = "Twitch",
		//		UserName = e.ChatMessage.Username
		//	});

		//}

		//internal void Service_OnNewFollowersDetected(object sender,
		//TwitchLib.Events.Services.FollowerService.OnNewFollowersDetectedArgs e)
		//{
		//	Interlocked.Exchange(ref _CurrentFollowerCount, _CurrentFollowerCount + e.NewFollowers.Count);
		//	Logger.LogInformation($"New Followers on Twitch, new total: {_CurrentFollowerCount}");

		//	Updated?.Invoke(this, new ServiceUpdatedEventArgs
		//	{
		//		ServiceName = Name,
		//		NewFollowers = _CurrentFollowerCount
		//	});
		//}

		private Task StopTwitchMonitoring()
		{

			Proxy.Dispose();
			_ChatClient.Dispose();

			return Task.CompletedTask;
		}

		public Task<bool> SendMessageAsync(string message)
		{
			_ChatClient.PostMessage(message);
			return Task.FromResult(true);
		}

		public Task<bool> SendWhisperAsync(string userName, string message)
		{

			_ChatClient.WhisperMessage(message, userName);
			return Task.FromResult(true);

		}

		public Task<bool> TimeoutUserAsync(string userName, TimeSpan time)
		{

			//_TwitchClient.TimeoutUser(userName, time);
			//return Task.FromResult(true);
			return Task.FromResult(false);


		}

		public Task<bool> BanUserAsync(string userName)
		{
			//_TwitchClient.BanUser(userName);
			//return Task.FromResult(true);
			return Task.FromResult(false);

		}

		public Task<bool> UnbanUserAsync(string userName)
		{
			//_TwitchClient.UnbanUser(userName);
			//return Task.FromResult(true);
			return Task.FromResult(false);
		}

		internal void MessageReceived(bool isModerator, bool isBroadcaster, string message, string userName)
		{
			ChatMessage?.Invoke(this, new ChatMessageEventArgs
			{
				IsModerator = isModerator,
				IsOwner = isBroadcaster,
				IsWhisper = false,
				Message = message,
				ServiceName = "Twitch",
				UserName = userName
			});

		}

	}

}
