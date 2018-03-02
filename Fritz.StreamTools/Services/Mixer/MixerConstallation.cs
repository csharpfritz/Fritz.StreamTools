using System;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

// https://dev.mixer.com/reference/constellation/index.html

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerConstallation : IDisposable
	{
		event EventHandler<ConstallationEventArgs> ConstallationEvent;
		Task ConnectAndJoinAsync(int channelId);
	}

	internal class MixerConstallation : IMixerConstallation
	{
		const string WS_URL = "wss://constellation.mixer.com";

		readonly IConfiguration _config;
		readonly ILoggerFactory _loggerFactory;
		readonly IMixerFactory _factory;
		readonly CancellationToken _shutdown;
		readonly ILogger _logger;
		IJsonRpcWebSocket _channel;

		public MixerConstallation(IConfiguration config, ILoggerFactory loggerFactory, IMixerFactory factory, CancellationToken shutdown)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_shutdown = shutdown;
			_logger = loggerFactory.CreateLogger(nameof(MixerConstallation));
		}

		/// <summary>
		/// Raised each time a chat message is received
		/// </summary>
		public event EventHandler<ConstallationEventArgs> ConstallationEvent;

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

			_channel = _factory.CreateJsonRpcWebSocket(_logger, isChat: false);

			// Connect to the chat endpoint
			var continueTrying = true;
			while (continueTrying && !await _channel.TryConnectAsync(() => WS_URL, token, async () =>	{
				// Join the channel and request live updates
				continueTrying = await _channel.SendAsync("livesubscribe", $"channel:{channelId}:update");
			}));

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
			if(e.Event == "live")
			{
				var payload = e.Data["payload"];
				if (payload.IsNullOrEmpty()) return;

				var e2 = new ConstallationEventArgs {
					FollowerCount = payload["numFollowers"]?.Value<int>(),
					ViewerCount = payload["viewersCurrent"]?.Value<int>(),
					IsOnline = payload["online"]?.Value<bool>()
				};
				if (e2.FollowerCount.HasValue || e2.ViewerCount.HasValue || e2.IsOnline.HasValue)
				{
					ConstallationEvent?.Invoke(this, e2);
				}
			}
		}

		public void Dispose()
		{
			_channel?.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	public class ConstallationEventArgs : EventArgs
	{
		public int? FollowerCount { get; set; }
		public int? ViewerCount { get; set; }
		public bool? IsOnline { get; set; }
	}
}
