using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

// https://dev.mixer.com/reference/constellation/index.html

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerConstellation : IDisposable
	{
		event EventHandler<ConstellationEventArgs> ConstellationEvent;
		Task ConnectAndJoinAsync(uint channelId);
	}

	internal class MixerConstellation : IMixerConstellation
	{
		const string WS_URL = "wss://constellation.mixer.com";

		readonly IConfiguration _config;
		readonly ILoggerFactory _loggerFactory;
		readonly IMixerFactory _factory;
		readonly CancellationToken _shutdown;
		readonly ILogger _logger;
		IJsonRpcWebSocket _channel;

		public MixerConstellation(IConfiguration config, ILoggerFactory loggerFactory, IMixerFactory factory, CancellationToken shutdown)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_shutdown = shutdown;
			_logger = loggerFactory.CreateLogger(nameof(MixerConstellation));
		}

		/// <summary>
		/// Raised each time a chat message is received
		/// </summary>
		public event EventHandler<ConstellationEventArgs> ConstellationEvent;

		/// <summary>
		/// Connect to the live event server, and join our channel
		/// </summary>
		/// <param name="channelId">Out channelId</param>
		/// <returns></returns>
		public async Task ConnectAndJoinAsync(uint channelId)
		{
			// Include token on connect if available
			var token = _config["StreamServices:Mixer:Token"];
			if (string.IsNullOrWhiteSpace(token))
				token = null;

			_channel = _factory.CreateJsonRpcWebSocket(_logger, isChat: false);

			// Connect to the chat endpoint
			var continueTrying = true;
			while (continueTrying && !await _channel.TryConnectAsync(() => WS_URL, token, async () => {
				// Join the channel and subscribe to events
				continueTrying = await _channel.SendAsync("livesubscribe",
						$"channel:{channelId}:update",
						$"channel:{channelId}:followed",
						$"channel:{channelId}:hosted",
						$"channel:{channelId}:unhosted",
						$"channel:{channelId}:subscribed",
						$"channel:{channelId}:resubscribed",
						$"channel:{channelId}:resubShared"
					);
			}))
				;

			if (!continueTrying)
			{
				_logger.LogError("Failed to connect to live endpoint {0}, giving up! (Channel wrong?)", WS_URL);
				_channel.Dispose();
				_channel = null;
				return;
			}

			_channel.EventReceived += EventReceived;
		}

		/// <summary>
		/// Called when we receive a new live event from server
		/// </summary>
		private void EventReceived(object sender, EventEventArgs e)
		{
			if (e.Event == "live")
			{
				var channel = e.Data["channel"]?.Value<string>().Split(':');
				if (channel == null || channel.Length == 0)
					return;
				var payload = e.Data["payload"];
				if (payload == null || payload.Type != JTokenType.Object)
					return;

				if (channel[0] == "channel")
				{
					var channelId = uint.Parse(channel[1]);
					ConstellationEvent?.Invoke(this, new ConstellationEventArgs { ChannelId = channelId, Event = channel.Last(), Payload = payload });
				}
			}
		}

		public void Dispose()
		{
			_channel?.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	public class ConstellationEventArgs : EventArgs
	{
		public uint ChannelId { get; set; }
		public string Event { get; set; }
		public JToken Payload { get; set; }
	}
}
