using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MixerLib
{
	public interface IMixerFactory
	{
		IClientWebSocketProxy CreateClientWebSocket(bool isChat);
		IMixerConstellation CreateConstellation(IEventParser parser, CancellationToken shutdownRequest);
		IMixerChat CreateChat(IMixerRestClient client, IEventParser parser, CancellationToken shutdownRequest);
		IJsonRpcWebSocket CreateJsonRpcWebSocket(ILogger logger, IEventParser parser);
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
		public IMixerConstellation CreateConstellation(IEventParser parser, CancellationToken shutdownRequest) => new MixerConstellation(_config, _loggerFactory, this, parser, shutdownRequest);
		public IMixerChat CreateChat(IMixerRestClient client, IEventParser parser, CancellationToken shutdownRequest) => new MixerChat(_config, _loggerFactory, this, client, parser, shutdownRequest);
		public IJsonRpcWebSocket CreateJsonRpcWebSocket(ILogger logger, IEventParser parser) => new JsonRpcWebSocket(logger, this, _config, parser);
		public IMixerRestClient CreateRestClient() => new MixerRestClient(_loggerFactory, new HttpClient());
	}
}
