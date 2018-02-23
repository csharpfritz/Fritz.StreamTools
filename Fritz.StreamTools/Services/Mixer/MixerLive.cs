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
		event EventHandler<EventEventArgs> OnLiveEvent;
		Task ConnectAndJoinAsync(int channelId);
	}

	public class MixerLive : IMixerLive
	{
		const string WS_URL = "wss://constellation.mixer.com";
		const int RECONNECT_DELAY = 10;

		readonly IConfiguration _config;
		readonly ILoggerFactory _loggerFactory;
		readonly IMixerAuth _auth;
		readonly HttpClient _client;
		readonly CancellationToken _shutdown;
		readonly ILogger _logger;
		JsonRpcWebSocket _channel;

		public MixerLive(IConfiguration config, ILoggerFactory loggerFactory, IMixerAuth auth, HttpClient client, CancellationToken shutdown)
		{
			_config = config;
			_loggerFactory = loggerFactory;
			_auth = auth;
			_client = client;
			_shutdown = shutdown;
			_logger = loggerFactory.CreateLogger<MixerLive>();
		}

		/// <summary>
		/// Raised each time a chat message is received
		/// </summary>
		public event EventHandler<EventEventArgs> OnLiveEvent;

		/// <summary>
		/// Connect to the live event server, and join our channel
		/// </summary>
		/// <param name="channelId">Out channelId</param>
		/// <returns></returns>
		public async Task ConnectAndJoinAsync(int channelId)
		{
			// We need a access_token for this to succeed
			if (string.IsNullOrEmpty(_auth.AccessToken)) return;

			_channel = new JsonRpcWebSocket(_loggerFactory, isChat: false);

			// Connect to the chat endpoint
			while (true)
			{
				if (await _channel.TryConnectAsync(() => WS_URL, _auth.AccessToken, () => {
					// Join the channel and request live updates
					return _channel.SendAsync("livesubscribe", $"channel:{channelId}:update");
				}))
				{
					break;
				}
			}

			_channel.OnEventReceived += Chat_OnEventReceived;
		}

		/// <summary>
		/// Called when we receive a new live event from server
		/// </summary>
		private void Chat_OnEventReceived(object sender, EventEventArgs e)
		{
			if(e.Event == "live")
			{
				Debug.Assert(e.Data["payload"] != null);
				OnLiveEvent?.Invoke(this, new EventEventArgs { Event = e.Event, Data = e.Data["payload"] });
			}
		}
	}
}
