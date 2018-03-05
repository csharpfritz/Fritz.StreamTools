using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerFactory
	{
		IClientWebSocketProxy CreateClientWebSocket(bool isChat);
		IMixerConstellation CreateConstellation(CancellationToken shutdownRequest);
		IMixerChat CreateChat(IMixerRestClient client, CancellationToken shutdownRequest);
		IJsonRpcWebSocket CreateJsonRpcWebSocket(ILogger logger, bool isChat);
		IMixerRestClient CreateRestClient();
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

		public IClientWebSocketProxy CreateClientWebSocket(bool isChat) => new ClientWebSocketProxy(isChat);
		public IMixerConstellation CreateConstellation(CancellationToken shutdownRequest) => new MixerConstellation(_config, _loggerFactory, this, shutdownRequest);
		public IMixerChat CreateChat(IMixerRestClient client, CancellationToken shutdownRequest) => new MixerChat(_config, _loggerFactory, this, client, shutdownRequest);
		public IJsonRpcWebSocket CreateJsonRpcWebSocket(ILogger logger, bool isChat) => new JsonRpcWebSocket(logger, this, _config, isChat);
		public IMixerRestClient CreateRestClient() => new MixerRestClient(_loggerFactory, new HttpClient());
	}
}
