using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// https://dev.mixer.com/reference/chat/index.html

namespace MixerLib
{
	public interface IMixerChat : IDisposable
	{
		bool IsAuthenticated { get; }
		string[] Roles { get; }
		Task ConnectAndJoinAsync(uint userId, uint channelId);
		Task<bool> SendWhisperAsync(string userName, string message);
		Task<bool> SendMessageAsync(string message);
		Task<bool> TimeoutUserAsync(string userName, TimeSpan time);
	}

	internal class MixerChat : IMixerChat
	{
		readonly IConfiguration _config;
		readonly ILoggerFactory _loggerFactory;
		readonly IMixerFactory _factory;
		readonly IMixerRestClient _restClient;
		readonly CancellationToken _shutdown;
		readonly ILogger _logger;
		uint _myUserId;
		IJsonRpcWebSocket _channel;
		readonly IEventParser _parser;

		public bool IsAuthenticated => _channel.IsAuthenticated;
		public string[] Roles => _channel.Roles;

		public MixerChat(IConfiguration config, ILoggerFactory loggerFactory, IMixerFactory factory, IMixerRestClient client,
			IEventParser parser, CancellationToken shutdown)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_restClient = client ?? throw new ArgumentNullException(nameof(client));
			_logger = loggerFactory.CreateLogger(nameof(MixerChat));
			_parser = parser ?? throw new ArgumentNullException(nameof(parser));
			_shutdown = shutdown;
		}

		/// <summary>
		/// Connect to the chat server, and join our channel
		/// </summary>
		/// <param name="userId">Our userId</param>
		/// <param name="channelId">Out channelId</param>
		/// <returns></returns>
		public async Task ConnectAndJoinAsync(uint userId, uint channelId)
		{
			var token = _config["StreamServices:Mixer:Token"];

			_myUserId = userId;
			_channel = _factory.CreateJsonRpcWebSocket(_logger, _parser);

			// Get chat authkey and endpoints
			var chatData = await _restClient.GetChatAuthKeyAndEndpointsAsync();
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
					chatData = await _restClient.GetChatAuthKeyAndEndpointsAsync();
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

			if (!continueTrying)
			{
				_logger.LogError("Failed to connect to chat endpoint, giving up! (Channel or Token wrong?)");
				_channel.Dispose();
				_channel = null;
				return;
			}
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
			if (success)
				_logger.LogTrace($"Send message '{message}'");
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
			if (success)
				_logger.LogTrace($"Send whisper to {userName} '{message}'");
			return success;
		}

		public async Task<bool> TimeoutUserAsync(string userName, TimeSpan time)
		{
			if (string.IsNullOrWhiteSpace(userName))
				throw new ArgumentException("Must not be null or empty", nameof(userName));

			if (!IsAuthenticated)
				return false;

			var success = await _channel.SendAsync("timeout", userName, $"{time.Minutes}m{time.Seconds}s");
			if (success)
				_logger.LogWarning($"TIMEOUT {userName} on Mixer for {time}");
			return success;
		}

		public void Dispose()
		{
			// Don't dispose _restClient here!

			_channel?.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
