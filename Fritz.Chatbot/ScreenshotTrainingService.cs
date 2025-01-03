using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.Chatbot
{
	public class ScreenshotTrainingService : IHostedService
	{
		public const int DefaultTrainingCount = 15;
		private int _TrainingPicCount = 15;
		public const int TrainingIntervalInSeconds = 2;

		// TODO: Track how many images are loaded -- 5k is the maximum for the FREE service
		private string _CustomVisionKey = "";
		private string _AzureEndpoint = "";
		private Guid _AzureProjectId;
		private ILogger _Logger;
		private IServiceProvider _Services;
		private CancellationTokenSource _TokenSource;


		private readonly Queue<MemoryStream> _ImagesToUpload = new Queue<MemoryStream>();

		public ScreenshotTrainingService(IConfiguration configuration, ILoggerFactory loggerFactory, IServiceProvider services)
		{

			_CustomVisionKey = configuration["AzureServices:HatDetection:Key"];
			_AzureEndpoint = configuration["AzureServices:HatDetection:CustomVisionEndpoint"];
			_AzureProjectId = Guid.Parse(configuration["AzureServices:HatDetection:ProjectId"]);
			_Logger = loggerFactory.CreateLogger("ScreenshotTraining");
			_Services = services;
		}


		public Task StartAsync(CancellationToken cancellationToken)
		{

			_TokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			return Task.CompletedTask;

		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{

			_TokenSource?.Cancel();

		}

		internal Task<Stream> GetScreenshotFromObs()
		{
			var source = new TaskCompletionSource<Stream>();

			var cancellationSource = new CancellationTokenSource(100 * 100);
			cancellationSource.Token.Register(() => source.TrySetCanceled());

			ScreenshotSink.Instance.ScreenshotReceived += (obj, args) =>
			{
				source.TrySetResult(args.Screenshot);
			};

			var scope = _Services.CreateScope();
			var obsContext = scope.ServiceProvider.GetRequiredService<IHubContext<ObsHub, ITakeScreenshots>>();
			_ = obsContext.Clients.All.TakeScreenshot().ContinueWith((t) =>
			{
				try
				{
					if (t.IsFaulted)
					{
						source.TrySetException(t.Exception.InnerExceptions);
						return;
					}

					if (t.IsCanceled || cancellationSource.IsCancellationRequested)
					{
						source.TrySetCanceled();
						return;
					}
				}
				finally
				{
					scope.Dispose();
				}
			});

			return source.Task;
		}

	}
}
