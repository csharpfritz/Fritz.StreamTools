using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// https://dev.mixer.com/reference/constellation/index.html

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerLive
	{
		event EventHandler<EventEventArgs> LiveEvent;
		Task ConnectAndJoinAsync(int channelId);
	}

	public class MixerLive : IMixerLive
	{
		const string WS_URL = "wss://constellation.mixer.com";
		const int RECONNECT_DELAY = 10;

		readonly IConfiguration _config;
		readonly ILoggerFactory _loggerFactory;
		readonly HttpClient _client;
		readonly CancellationToken _shutdown;
		readonly ILogger _logger;
		JsonRpcWebSocket _channel;

		public MixerLive(IConfiguration config, ILoggerFactory loggerFactory, HttpClient client, CancellationToken shutdown)
		{
			_config = config;
			_loggerFactory = loggerFactory;
			_client = client;
			_shutdown = shutdown;
			_logger = loggerFactory.CreateLogger("MixerLive");
		}

		/// <summary>
		/// Raised each time a chat message is received
		/// </summary>
		public event EventHandler<EventEventArgs> LiveEvent;

		/// <summary>
		/// Connect to the live event server, and join our channel
		/// </summary>
		/// <param name="channelId">Out channelId</param>
		/// <returns></returns>
		public async Task ConnectAndJoinAsync(int channelId)
		{
			// Include token on connect if available
			var token = _config["StreamServices:Mixer:Token"];
			if (string.IsNullOrWhiteSpace(token)) token = null;

			_channel = new JsonRpcWebSocket(_logger, isChat: false);

			// Connect to the chat endpoint
			while (true)
			{
				if (await _channel.TryConnectAsync(() => WS_URL, token, () => {
					// Join the channel and request live updates
					return _channel.SendAsync("livesubscribe", $"channel:{channelId}:update");
				}))
				{
					break;
				}
			}

			_channel.EventReceived += Chat_EventReceived;
		}

		/// <summary>
		/// Called when we receive a new live event from server
		/// </summary>
		private void Chat_EventReceived(object sender, EventEventArgs e)
		{
			if(e.Event == "live")
			{
				Debug.Assert(e.Data["payload"] != null);
				LiveEvent?.Invoke(this, new EventEventArgs { Event = e.Event, Data = e.Data["payload"] });
			}
		}
	}
}
