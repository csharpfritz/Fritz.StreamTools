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
		private readonly BotClient _BotClient;

		public Worker(ILogger<Worker> logger, ObsClient obsClient, BotClient botClient)
		{
			_logger = logger;
			_ObsClient = obsClient;
			_BotClient = botClient;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{

			await _ObsClient.Connect(4444);
			await _BotClient.Connect();

			while (!stoppingToken.IsCancellationRequested)
			{
				await Task.Delay(1000, stoppingToken);
				Console.WriteLine($"Current time is: {DateTime.Now} and we are {_BotClient.ConnectedState} to the ChatBot");
			}
		}
	}
}
