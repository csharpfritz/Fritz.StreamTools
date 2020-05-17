using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fritz.ObsProxy
{
	public class Worker : IHostedService
	{
		private readonly ILogger<Worker> _logger;
		private readonly ObsClient _ObsClient;
		private readonly BotClient _BotClient;

		public Worker(ILogger<Worker> logger, ObsClient obsClient, BotClient botClient)
		{
			_logger = logger;
			_ObsClient = obsClient;
			_BotClient = botClient;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{

			await _ObsClient.Connect();
			await _BotClient.Connect();

		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{

			_ObsClient.Dispose();
			await _BotClient.DisposeAsync();

		}

	}
}
