using Fritz.StreamLib.Core;
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

	public class BotClient : IAsyncDisposable, ITakeScreenshots
	{
		private readonly Uri _BotUrl;
		private readonly ObsClient _ObsClient;
		private HubConnection _Client;

		public BotClient(ILoggerFactory loggerFactory, IConfiguration configuration, ObsClient obsClient)
		{

			_BotUrl = new Uri(configuration["BotUrl"]);
			_ObsClient = obsClient;
		}

		public async Task Connect() {

			_Client = new HubConnectionBuilder()
				.WithUrl(_BotUrl)
				.WithAutomaticReconnect()
				.AddJsonProtocol()
				.Build();


			_Client.On("TakeScreenshot", TakeScreenshot);

			await _Client.StartAsync();

			await Task.Delay(100);
			var i = 0;
			while (_Client.State != HubConnectionState.Connected)
			{
				if (i > 20) { break; }
				i++;
				await Task.Delay(100);
				await _Client.StartAsync();
			}

		}

		public HubConnectionState ConnectedState => _Client.State;

		public async ValueTask DisposeAsync()
		{
			await _Client.DisposeAsync();
		}

		public async Task TakeScreenshot()
		{

			var result = _ObsClient.TakeScreenshot();
			await _Client.InvokeAsync("PostScreenshot", result);

		}
	}

}
