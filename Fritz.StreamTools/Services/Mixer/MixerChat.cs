using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

// https://dev.mixer.com/reference/chat/index.html

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerChat : IDisposable
	{
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

		public MixerChat(IConfiguration config, ILoggerFactory loggerFactory, IMixerFactory factory, IMixerRestClient client, CancellationToken shutdown)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_client = client ?? throw new ArgumentNullException(nameof(client));
			_shutdown = shutdown;
			_logger = loggerFactory.CreateLogger(nameof(MixerChat));
		}

		/// <summary>
		/// Raised each time a chat message is received
		/// </summary>
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
			// We need a access_token for this to succeed
			var token = _config["StreamServices:Mixer:Token"];
			if (string.IsNullOrEmpty(token)) return;

			_myUserId = userId;

			// Get chat authkey and endpoints
			var chatData = await _client.GetChatAuthKeyAndEndpointsAsync();

			_channel = _factory.CreateJsonRpcWebSocket(_logger, isChat: true);
			var endpointIndex = Math.Min(1, chatData.Endpoints.Length - 1); // Skip 1st one, seems to fail often

			// Chose next endpoint
			string getNextEnpoint()
			{
				var endpoint = chatData.Endpoints[endpointIndex];
				endpointIndex = (endpointIndex + 1) % chatData.Endpoints.Length;
				return chatData.Endpoints[endpointIndex];
			}

			// Connect to the chat endpoint
			while (!await _channel.TryConnectAsync(getNextEnpoint, null, async () => {
				// Join the channel and send authkey
				var succeeded = await _channel.SendAsync("auth", channelId, userId, chatData.AuthKey);
				if (!succeeded)
				{
					// Try again with a new chatAuthKey
					chatData = await _client.GetChatAuthKeyAndEndpointsAsync();
					endpointIndex = Math.Min(1, chatData.Endpoints.Length - 1);

					// If this fail give up !
					await _channel.SendAsync("auth", channelId, userId, chatData.AuthKey);
				}
			}));

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

			var success = await _channel.SendAsync("whisper", userName, message);
			if(success) _logger.LogTrace($"Send whisper to {userName} '{message}'");
			return success;
		}

		public async Task<bool> TimeoutUserAsync(string userName, TimeSpan time)
		{
			if (string.IsNullOrWhiteSpace(userName))
				throw new ArgumentException("Must not be null or empty", nameof(userName));

			var success = await _channel.SendAsync("timeout", userName, $"{time.Minutes}m{time.Seconds}s");
			if (success) _logger.LogWarning($"TIMEOUT {userName} on Mixer for {time}");
			return success;
		}

		/// <summary>
		/// Called when we receive a new event from the chat server
		/// </summary>
		private void EventReceived(object sender, EventEventArgs e)
		{
			if(e.Event == "ChatMessage")
			{
				var userId = e.Data["user_id"].Value<int>();
				var roles = e.Data["user_roles"].Values<string>();

				// Combine text from all elements
				var segments = e.Data["message"]["message"];
				var combinedText = string.Join("", segments.Where(x => x["text"] != null).Select(x => (string)x["text"]));

				var isWhisper = false;
				var meta = e.Data["message"]["meta"];
				if(!meta.IsNullOrEmpty() && !meta["whisper"].IsNullOrEmpty())
				{
					// "meta":{"whisper":true}},"target":"jobun44"}
					isWhisper = (bool)meta["whisper"];
				}

				ChatMessage?.Invoke(this, new ChatMessageEventArgs
				{
					UserId = userId,
					UserName = e.Data["user_name"].Value<string>(),
					IsWhisper = isWhisper,
					IsModerator = roles.Contains("Mod"),
					IsOwner = roles.Contains("Owner"),
					Message = combinedText
				});
			}
			else if(e.Event == "UserJoin")
			{
				UserJoined?.Invoke(this, new ChatUserInfoEventArgs { UserId = (int)e.Data["id"], UserName = (string)e.Data["username"] });
			}
			else if (e.Event == "UserLeave")
			{
				UserLeft?.Invoke(this, new ChatUserInfoEventArgs { UserId = (int)e.Data["id"], UserName = (string)e.Data["username"] });
			}
		}

		public void Dispose()
		{
			// Dont dispose _client here!
			_channel.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
