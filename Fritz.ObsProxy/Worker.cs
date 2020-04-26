using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fritz.ObsProxy
{
	public class Worker : BackgroundService
	{
		private readonly ILogger<Worker> _logger;
		private readonly ObsClient _ObsClient;

		public Worker(ILogger<Worker> logger, ObsClient obsClient)
		{
			_logger = logger;
			_ObsClient = obsClient;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{

			await _ObsClient.Connect(4444);
			_ObsClient.Identify();

			while (!stoppingToken.IsCancellationRequested)
			{
				_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
				await Task.Delay(1000, stoppingToken);
			}
		}
	}
}
