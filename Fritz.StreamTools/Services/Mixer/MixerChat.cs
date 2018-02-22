using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

// https://dev.mixer.com/reference/chat/index.html

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerChat
	{
		event EventHandler<ChatMessageEventArgs> OnChatMessage;
		Task ConnectAndJoinAsync(int userId, int channelId);
	}

	public class ChatMessageEventArgs : EventArgs
	{
		public int UserId { get; set; }
		public string UserName { get; set; }
		public string Message { get; set; }
	}

	public class MixerChat : IMixerChat
	{
		readonly IConfiguration _config;
		readonly ILoggerFactory _loggerFactory;
		readonly IMixerAuth _auth;
		readonly HttpClient _client;
		readonly CancellationToken _shutdown;
		readonly ILogger _logger;
		int _myUserId;

		public MixerChat(IConfiguration config, ILoggerFactory loggerFactory, IMixerAuth auth, HttpClient client, CancellationToken shutdown)
		{
			_config = config;
			_loggerFactory = loggerFactory;
			_auth = auth;
			_client = client;
			_shutdown = shutdown;
			_logger = loggerFactory.CreateLogger<MixerChat>();
		}

		public event EventHandler<ChatMessageEventArgs> OnChatMessage;

		public async Task ConnectAndJoinAsync(int userId, int channelId)
		{
			// We need a access_token for this to succeed
			if (string.IsNullOrEmpty(_auth.AccessToken)) return;

			_myUserId = userId;

			// Get chat authkey and chat endpoints
			var response = await _client.GetStringAsync($"chats/{channelId}");
			var doc = JToken.Parse(response);
			var chatAuthKey = doc["authkey"].Value<string>();
			var endpoints = doc["endpoints"].Values<string>().ToArray();

			var chat = new JsonRpcWebSocket(_loggerFactory);
			var endpointIndex = 0;

			// Connect to the chat endpoint
			while (true)
			{
				var endpoint = endpoints[endpointIndex];
				endpointIndex = (endpointIndex + 1) % endpoints.Length;
				_logger.LogInformation($"Connecting to {endpoint}");

				if (await chat.TryConnectAsync(endpoint))
				{
					_logger.LogInformation($"Connecting to {endpoint} succeeded");
					break;
				}
			}

			chat.OnEventReceived += Chat_OnEventReceived;

			// Join the channel and send authkey
			await chat.SendAsync("auth", channelId, userId, chatAuthKey);
		}

		/// <summary>
		/// Called when we receive a new event from the chat server
		/// </summary>
		private void Chat_OnEventReceived(object sender, EventEventArgs e)
		{
			if(e.Event == "ChatMessage")
			{
				var userId = e.Data["user_id"].Value<int>();

#if false
				// Ignore my own messages
				if (userId == _myUserId)
					return;
#endif
				var text = e.Data["message"]["message"][0]["text"].Value<string>();
				OnChatMessage?.Invoke(this, new ChatMessageEventArgs
				{
					UserId = userId,
					UserName = e.Data["user_name"].Value<string>(),
					Message = text
				});
			}
		}
	}
}
