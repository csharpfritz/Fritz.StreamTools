using System;
using System.Net.WebSockets;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerFactory
	{
		ClientWebSocket CreateClientWebSocket();
		IMixerLive CreateMixerLive(CancellationToken shutdownRequest);
		IMixerChat CreateMixerChat(IMixerRestClient client, CancellationToken shutdownRequest);
		IJsonRpcWebSocket CreateJsonRpcWebSocket(ILogger logger, bool isChat);
		IMixerRestClient CreateMixerRestClient(string channelName, string token = null);
	}

	internal class MixerFactory : IMixerFactory
  {
		private readonly IConfiguration _config;
		private readonly ILoggerFactory _loggerFactory;

		public MixerFactory(IConfiguration config, ILoggerFactory loggerFactory)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
		}

		public IMixerLive CreateMixerLive(CancellationToken shutdownRequest) => new MixerLive(_config, _loggerFactory, this, shutdownRequest);
		public IMixerChat CreateMixerChat(IMixerRestClient client, CancellationToken shutdownRequest) => new MixerChat(_config, _loggerFactory, this, client, shutdownRequest);
		public IJsonRpcWebSocket CreateJsonRpcWebSocket(ILogger logger, bool isChat) => new JsonRpcWebSocket(logger, this, isChat);
		public ClientWebSocket CreateClientWebSocket() => new ClientWebSocket();
		public IMixerRestClient CreateMixerRestClient(string channelName, string token) => new MixerRestClient(_loggerFactory, channelName, token);
	}
}
