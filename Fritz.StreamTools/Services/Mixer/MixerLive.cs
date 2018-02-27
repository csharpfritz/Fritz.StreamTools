using System;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// https://dev.mixer.com/reference/constellation/index.html

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerLive : IDisposable
	{
		event EventHandler<LiveEventArgs> LiveEvent;
		Task ConnectAndJoinAsync(int channelId);
	}

	internal class MixerLive : IMixerLive
	{
		const string WS_URL = "wss://constellation.mixer.com";

		readonly IConfiguration _config;
		readonly ILoggerFactory _loggerFactory;
		readonly IMixerFactory _factory;
		readonly CancellationToken _shutdown;
		readonly ILogger _logger;
		IJsonRpcWebSocket _channel;

		public MixerLive(IConfiguration config, ILoggerFactory loggerFactory, IMixerFactory factory, CancellationToken shutdown)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_shutdown = shutdown;
			_logger = loggerFactory.CreateLogger(nameof(MixerLive));
		}

		/// <summary>
		/// Raised each time a chat message is received
		/// </summary>
		public event EventHandler<LiveEventArgs> LiveEvent;

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
			while (!await _channel.TryConnectAsync(() => WS_URL, token, () =>	{
				// Join the channel and request live updates
				return _channel.SendAsync("livesubscribe", $"channel:{channelId}:update");
			}).ConfigureAwait(false))
				;

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

				var e2 = new LiveEventArgs();
				if (payload["numFollowers"] != null) e2.FollowerCount = (int)payload["numFollowers"];
				if (payload["viewersCurrent"] != null) e2.ViewerCount = (int)payload["viewersCurrent"];
				if (payload["online"] != null) e2.IsOnline = (bool)payload["online"];
				if(e2.FollowerCount.HasValue || e2.ViewerCount.HasValue || e2.IsOnline.HasValue)
				{
					LiveEvent?.Invoke(this, e2);
				}
			}
		}

		public void Dispose()
		{
			_channel.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	public class LiveEventArgs : EventArgs
	{
		public int? FollowerCount { get; set; }
		public int? ViewerCount { get; set; }
		public bool? IsOnline { get; set; }
	}
}
