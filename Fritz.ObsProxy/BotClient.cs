using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.ObsProxy
{

	public class BotClient : IAsyncDisposable
	{
		private readonly Uri _BotUrl;
		private HubConnection _Client;

		public BotClient(ILoggerFactory loggerFactory, IConfiguration configuration)
		{

			_BotUrl = new Uri(configuration["BotUrl"]);

		}

		public async Task Connect() {

			_Client = new HubConnectionBuilder()
				.WithUrl(_BotUrl)
				.WithAutomaticReconnect()
				.AddJsonProtocol()
				.Build();

			await _Client.StartAsync();

		}

		public async ValueTask DisposeAsync()
		{
			await _Client.DisposeAsync();
		}
	}

}
