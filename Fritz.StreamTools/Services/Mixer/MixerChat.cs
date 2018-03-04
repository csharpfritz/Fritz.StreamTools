using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// https://dev.mixer.com/reference/chat/index.html

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerChat : IDisposable
	{
		bool IsAuthenticated { get; }
		event EventHandler<ChatMessageEventArgs> ChatMessage;
		event EventHandler<ChatUserInfoEventArgs> UserJoined;
		event EventHandler<ChatUserInfoEventArgs> UserLeft;

		Task ConnectAndJoinAsync(int userId, int channelId);
		Task<bool> SendWhisperAsync(string userName, string message);
		Task<bool> SendMessageAsync(string message);
		Task<bool> TimeoutUserAsync(string userName, TimeSpan time);
	}

	internal class MixerChat : IMixerChat
	{
		readonly IConfiguration _config;
		readonly ILoggerFactory _loggerFactory;
		readonly IMixerFactory _factory;
		readonly IMixerRestClient _client;
		readonly CancellationToken _shutdown;
		readonly ILogger _logger;
		int _myUserId;
		IJsonRpcWebSocket _channel;

		public bool IsAuthenticated => _channel.IsAuthenticated;

		public MixerChat(IConfiguration config, ILoggerFactory loggerFactory, IMixerFactory factory, IMixerRestClient client, CancellationToken shutdown)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_client = client ?? throw new ArgumentNullException(nameof(client));
			_shutdown = shutdown;
			_logger = loggerFactory.CreateLogger(nameof(MixerChat));
		}

		public event EventHandler<ChatMessageEventArgs> ChatMessage;
		public event EventHandler<ChatUserInfoEventArgs> UserJoined;
		public event EventHandler<ChatUserInfoEventArgs> UserLeft;

		/// <summary>
		/// Connect to the chat server, and join our channel
		/// </summary>
		/// <param name="userId">Our userId</param>
		/// <param name="channelId">Out channelId</param>
		/// <returns></returns>
		public async Task ConnectAndJoinAsync(int userId, int channelId)
		{
			var token = _config["StreamServices:Mixer:Token"];

			_myUserId = userId;

			// Get chat authkey and endpoints
			var chatData = await _client.GetChatAuthKeyAndEndpointsAsync();

			_channel = _factory.CreateJsonRpcWebSocket(_logger, isChat: true);
			var endpointIndex = Math.Min(1, chatData.Endpoints.Count - 1); // Skip 1st one, seems to fail often

			// Local function to choose the next endpoint to try
			string getNextEnpoint()
			{
				var endpoint = chatData.Endpoints[endpointIndex];
				endpointIndex = ( endpointIndex + 1 ) % chatData.Endpoints.Count;
				return chatData.Endpoints[endpointIndex];
			}

			var continueTrying = true;

			// Local function to join chat channel 
			async Task joinAndAuth()
			{
				if (string.IsNullOrEmpty(chatData.Authkey))
					continueTrying = await _channel.SendAsync("auth", channelId);  // Authenticating anonymously
				else
					continueTrying = await _channel.SendAsync("auth", channelId, userId, chatData.Authkey);
		  }

			// Local function to join chat channel
			async Task postConnect()
			{
				// Join the channel and send authkey
				await joinAndAuth();

				if (!continueTrying && !string.IsNullOrEmpty(chatData.Authkey))
				{
					// Try again with a new chatAuthKey
					chatData = await _client.GetChatAuthKeyAndEndpointsAsync();
					endpointIndex = Math.Min(1, chatData.Endpoints.Count - 1);

					await joinAndAuth(); // If this fail give up !
				}
			}

			// Connect to the chat endpoint
			while (continueTrying)
			{
				if (await _channel.TryConnectAsync(getNextEnpoint, null, postConnect))
					break;
			}

			if(!continueTrying)
			{
				_logger.LogError("Failed to connect to chat endpoint, giving up! (Channel or Token wrong?)");
				_channel.Dispose();
				_channel = null;
				return;
			}

			_channel.EventReceived += EventReceived;
		}

		//
		/// <summary>
		/// Send a chat message
		/// </summary>
		public async Task<bool> SendMessageAsync(string message)
		{
			if (string.IsNullOrEmpty(message))
				throw new ArgumentException("Must not be null or empty", nameof(message));

			if (!IsAuthenticated)
				return false;

			var success = await _channel.SendAsync("msg", message);
			if (success) _logger.LogTrace($"Send message '{message}'");
			return success;
		}

		/// <summary>
		/// Send a whisper message to a user
		/// </summary>
		public async Task<bool> SendWhisperAsync(string userName, string message)
		{
			if (string.IsNullOrWhiteSpace(userName))
				throw new ArgumentException("Must not be null or empty", nameof(userName));
			if (string.IsNullOrEmpty(message))
				throw new ArgumentException("Must not be null or empty", nameof(message));

			if (!IsAuthenticated)
				return false;

			var success = await _channel.SendAsync("whisper", userName, message);
			if(success) _logger.LogTrace($"Send whisper to {userName} '{message}'");
			return success;
		}

		public async Task<bool> TimeoutUserAsync(string userName, TimeSpan time)
		{
			if (string.IsNullOrWhiteSpace(userName))
				throw new ArgumentException("Must not be null or empty", nameof(userName));

			if (!IsAuthenticated)
				return false;

			var success = await _channel.SendAsync("timeout", userName, $"{time.Minutes}m{time.Seconds}s");
			if (success) _logger.LogWarning($"TIMEOUT {userName} on Mixer for {time}");
			return success;
		}

		/// <summary>
		/// Called when we receive a new event from the chat server
		/// </summary>
		private void EventReceived(object sender, EventEventArgs e)
		{
			switch (e.Event)
			{
				case "ChatMessage":
					ParseChatMessage(e);
					break;
				case "UserJoin":
					ParseUserJoin(e);
					break;
				case "UserLeave":
					ParseUserLeave(e);
					break;
			}
		}

		private void ParseUserLeave(EventEventArgs e)
		{
			var user = e.Data.GetObject<WS.User>();
			UserLeft?.Invoke(this, new ChatUserInfoEventArgs {
				UserId = user.UserId,
				UserName = user.Username,
				Properties = {
					{ "MixerRoles", user.Roles }
				}
			});
		}

		private void ParseUserJoin(EventEventArgs e)
		{
			var user = e.Data.GetObject<WS.User>();
			UserJoined?.Invoke(this, new ChatUserInfoEventArgs {
				UserId = user.UserId,
				UserName = user.Username,
				Properties = {
					{ "MixerRoles", user.Roles }
				}
			});
		}

		private void ParseChatMessage(EventEventArgs e)
		{
			var data = e.Data.GetObject<WS.ChatData>();

			var combinedText = string.Concat(data.Messages.Message.Select(x => x.Text));

			var isWhisper = false;
			if (data.Messages?.Meta != null)
			{
				isWhisper = data.Messages.Meta.Whisper.GetValueOrDefault();
			}

			ChatMessage?.Invoke(this, new ChatMessageEventArgs {
				UserId = data.UserId,
				UserName = data.UserName,
				IsWhisper = isWhisper,
				IsModerator = data.UserRoles.Contains("Mod"),
				IsOwner = data.UserRoles.Contains("Owner"),
				Message = combinedText,
				Properties = {
					{ "AvatarUrl", data.UserAvatar ?? string.Empty },
					{ "MixerUserLevel", data.UserLevel },
					{ "MixerRoles", data.UserRoles.ToArray() }
				}
			});
		}

		public void Dispose()
		{
			// Dont dispose _client here!
			_channel?.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
