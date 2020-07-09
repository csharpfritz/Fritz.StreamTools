using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.Chatbot
{
	public class ScreenshotTrainingService : IHostedService, ITrainHat
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

		private bool _CurrentlyTraining = false;
		private byte _TrainingCount = 0;
		private Task _TrainingTask;
		private byte _TotalPictures = 0;
		private byte _RetryCount = 0;

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

			_TrainingTask = Train(_TokenSource.Token);
			return Task.CompletedTask;

		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{

			_TokenSource?.Cancel();
			await _TrainingTask;

		}

		private async Task Train(CancellationToken token)
		{

			while (!token.IsCancellationRequested)
			{

				if (_CurrentlyTraining && _TotalPictures == DefaultTrainingCount)
				{
					_CurrentlyTraining = false;
					_Logger.LogTrace("Completed screenshot training");
					await UploadCachedScreenshots();
					if (!_CurrentlyTraining) _TotalPictures = 0;

				}
				else if (_CurrentlyTraining)
				{

					if (_ImagesToUpload.Count == 5) {
						await UploadCachedScreenshots();
					}

					var imageStream = await GetScreenshotFromObs();
					_TotalPictures++;
					_ImagesToUpload.Enqueue((MemoryStream)imageStream);
					await Task.Delay(TimeSpan.FromSeconds(TrainingIntervalInSeconds));

				}
				else
				{

					await Task.Delay(100);

				}

			}

		}

		private async Task UploadCachedScreenshots()
		{

			if (!_ImagesToUpload.Any()) return;

			var trainingClient = new CustomVisionTrainingClient()
			{
				ApiKey = _CustomVisionKey,
				Endpoint = _AzureEndpoint
			};

			var listToLoad = new List<ImageFileCreateEntry>();
			while (_ImagesToUpload.Any())
			{
				var imgStream = _ImagesToUpload.Dequeue();
				listToLoad.Add(new ImageFileCreateEntry(contents: imgStream.ToArray()));
			}

			var result = await trainingClient.CreateImagesFromFilesWithHttpMessagesAsync(_AzureProjectId, new ImageFileCreateBatch()
			{
				Images = listToLoad
			});

			_TotalPictures -= (byte)result.Body.Images.Where(r => r.Status != "OK").Count();
			if (_TotalPictures >= DefaultTrainingCount) _CurrentlyTraining = true;

			Console.WriteLine(result.ToString());

		}

		public void StartTraining(int? count)
		{

			if (_CurrentlyTraining) return;

			_Logger.LogTrace("Starting screenshot training");
			_TrainingCount = 0;
			_TrainingPicCount = count ?? DefaultTrainingCount;
			_CurrentlyTraining = true;

		}

		private async Task AddScreenshot(bool @internal)
		{

			if (!@internal && _CurrentlyTraining) return;

			try
			{
				var trainingClient = new CustomVisionTrainingClient()
				{
					ApiKey = _CustomVisionKey,
					Endpoint = _AzureEndpoint
				};

				var imageStream = await GetScreenshotFromObs();
				// TODO: If imageStream is null, handle gracefully

				var result = await trainingClient.CreateImagesFromDataAsync(_AzureProjectId,
					imageStream
				);

				if (!result.IsBatchSuccessful && _RetryCount < 3) {
					_Logger.LogWarning($"Error while adding screenshot #{_TrainingCount} - trying again in 10 seconds");
					await Task.Delay(TimeSpan.FromSeconds(10));
					_RetryCount++;
					await AddScreenshot(true);
					return;
				} else if (_RetryCount >= 3) {

					_Logger.LogError("Unable to add screenshot to Azure Custom Vision service");
					_RetryCount = 0;
					return;

				}

				_RetryCount = 0;

				if (_CurrentlyTraining)
				{
					_TrainingCount++;
					_Logger.LogTrace($"Successfully added screenshot #{_TrainingCount}");
				}

			}
			catch (Exception ex)
			{

			_Logger.LogError($"Error while adding screenshot: {ex.Message}");
			}

		}

		internal async Task<Stream> GetScreenshotFromObs()
		{

			Stream result = null;

			ScreenshotSink.Instance.ScreenshotReceived += (obj, args) =>
			{
				result = args.Screenshot;
			};

			using (var scope = _Services.CreateScope())
			{
				var obsContext = scope.ServiceProvider.GetRequiredService<IHubContext<ObsHub, ITakeScreenshots>>();
				await obsContext.Clients.All.TakeScreenshot();
			}
			var i = 0;
			while (result == null) {
				await Task.Delay(100);
				i++;
				if (i >= 100) break;
			}

			return result;

		}

		public Task AddScreenshot()
		{

			return AddScreenshot(false);

		}

	}
}
