using Fritz.StreamLib.Core;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Fritz.ObsProxy
{

	public class BotClient : IAsyncDisposable, ITakeScreenshots
	{
		private readonly ILogger _Logger;
		private readonly Uri _BotUrl;
		private readonly ObsClient _ObsClient;
		private HubConnection _Client;

		public BotClient(ILoggerFactory loggerFactory, IConfiguration configuration, ObsClient obsClient)
		{
			_Logger = loggerFactory.CreateLogger("BotClient");
			_BotUrl = new Uri(configuration["BotUrl"]);
			_ObsClient = obsClient;
		}

		public async Task Connect()
		{

			_Client = new HubConnectionBuilder()
				.WithUrl(_BotUrl)
				.WithAutomaticReconnect()
				.AddJsonProtocol()
				.Build();

			_Client.On("TakeScreenshot", TakeScreenshot);

			await StartAsync();

			// await _Client.SendAsync("PostScreenshot", "Foo");

			async Task StartAsync(int retryCount = 0)
			{

				try
				{
					await _Client.StartAsync();
				} catch (Exception ex) {
					// do nothing, we're gonna try again...
				}

				if (_Client.State == HubConnectionState.Connected)
				{
					_Logger.LogWarning("Connected to ObsHub");

				} 
				else if (retryCount < 20) {
					_Logger.LogWarning($"Retrying connection {retryCount}");
					retryCount++;
					await Task.Delay(100);
					await StartAsync(retryCount);
				}
			}
		}

		public HubConnectionState ConnectedState => _Client.State;

		public async ValueTask DisposeAsync()
		{

			if (_Client != null)
			{
				await _Client?.DisposeAsync();
			}

		}

		private Task _PostScreenshotTask;
		//private string 
		public Task TakeScreenshot()
		{

			var result = _ObsClient.TakeScreenshot();
			//_Client.
			//_PostScreenshotTask = Task.Run(() => _Client.SendAsync("PostScreenshot", result));

			_Client.SendAsync("PostScreenshot", clientStreamData(result));

			return Task.CompletedTask;

			async IAsyncEnumerable<string> clientStreamData(string imageData)
			{

				var sr = new StringReader(imageData);
				Debug.WriteLine($"ImageData ({imageData.Length}): " + imageData.Substring(imageData.Length-20,20));

				var buffer = new char[2000];
				while (true)
				{
					var lengthRead = sr.ReadBlock(buffer, 0, 2000);
					if (lengthRead == 0) { break; }
					yield return new string(buffer,0, lengthRead);
					buffer = new char[2000];
				}
				//After the for loop has completed and the local function exits the stream completion will be sent.
			}

		}
	}

}
