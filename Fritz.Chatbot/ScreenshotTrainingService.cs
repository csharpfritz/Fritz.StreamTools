using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.Chatbot
{
	public class ScreenshotTrainingService : IHostedService, ITrainHat
	{

		// TODO: Track how many images are loaded -- 5k is the maximum for the FREE service
		private string _CustomVisionKey = "";
		private string _AzureEndpoint = "";
		private string _TwitchChannel = "";
		private Guid _AzureProjectId;
		private readonly ILogger _Logger;
		private CancellationTokenSource _TokenSource;

		private bool _CurrentlyTraining = false;
		private byte _TrainingCount = 0;
		private Task _TrainingTask;
		private byte _RetryCount = 0;

		public string TwitchScreenshotUrl => $"https://static-cdn.jtvnw.net/previews-ttv/live_user_{_TwitchChannel}-1280x720.jpg?_=";

		public ScreenshotTrainingService(IConfiguration configuration, ILoggerFactory loggerFactory)
		{

			_CustomVisionKey = configuration["AzureServices:HatDetection:Key"];
			_AzureEndpoint = configuration["AzureServices:HatDetection:CustomVisionEndpoint"];
			_TwitchChannel = configuration["StreamServices:Twitch:Channel"];
			_AzureProjectId = Guid.Parse(configuration["AzureServices:HatDetection:ProjectId"]);
			_Logger = loggerFactory.CreateLogger("ScreenshotTraining");

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

				if (_CurrentlyTraining && _TrainingCount == 15)
				{
					_CurrentlyTraining = false;
					_Logger.LogTrace("Completed screenshot training");

				}
				else if (_CurrentlyTraining)
				{

					await AddScreenshot(true);
					await Task.Delay(TimeSpan.FromMinutes(1));

				}
				else
				{

					await Task.Delay(100);

				}

			}

		}

		public void StartTraining()
		{

			if (_CurrentlyTraining) return;

			_Logger.LogTrace("Starting screenshot training");
			_TrainingCount = 0;
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

				var result = await trainingClient.CreateImagesFromUrlsAsync(_AzureProjectId, new ImageUrlCreateBatch(
					new List<ImageUrlCreateEntry> {
					new ImageUrlCreateEntry(TwitchScreenshotUrl + Guid.NewGuid().ToString())
					}
				));

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


		public Task AddScreenshot()
		{

			return AddScreenshot(false);

		}

	}
}
