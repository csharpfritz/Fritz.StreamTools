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
		private readonly ConstellationEventProcessor _eventProcessor;

		public MixerConstellation(IConfiguration config, ILoggerFactory loggerFactory, IMixerFactory factory,
			ConstellationEventProcessor eventProcessor, CancellationToken shutdown)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_logger = loggerFactory.CreateLogger(nameof(MixerConstellation));
			_eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
			_shutdown = shutdown;
		}

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
			_channel.EventReceived += HandleEvents;

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
		}

		/// <summary>
		/// Called when we receive a new live event from server
		/// </summary>
		private void HandleEvents(object sender, EventEventArgs e)
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
					_eventProcessor.Process(channel.Last(), channelId, payload);
				}
			}
		}

		public void Dispose()
		{
			// Dont dispose _client here!

			if (_channel != null)
				_channel.EventReceived -= HandleEvents;

			_channel?.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
