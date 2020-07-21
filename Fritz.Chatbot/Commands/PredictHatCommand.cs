using Fritz.StreamLib.Core;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using System.Net;
using Microsoft.AspNetCore.SignalR;
using Fritz.StreamTools.Hubs;
using System.IO;
using System.Drawing;
using Fritz.Chatbot.ML.DataModels;
using Microsoft.Extensions.ML;

namespace Fritz.Chatbot.Commands
{
	public class PredictHatCommand : IBasicCommand
	{
		public string Trigger => "hat";
		public string Description => "Identify which hat Fritz is wearing";
		public TimeSpan? Cooldown => TimeSpan.FromSeconds(30);

		private string _CustomVisionKey = "";
		private string _AzureEndpoint = "";
		private string _TwitchChannel = "";
		private Guid _AzureProjectId;

		internal static string IterationName = "";
		private ScreenshotTrainingService _TrainHat;
		private readonly HatDescriptionRepository _Repository;
		private readonly IHubContext<ObsHub> _HubContext;
		private readonly PredictionEnginePool<ImageInputData, ImageLabelPredictions> _PredictionEnginePool;
		private readonly string _labelsFilePath;

		public PredictHatCommand(IConfiguration configuration, ScreenshotTrainingService service, HatDescriptionRepository repository, IHubContext<ObsHub> hubContext, PredictionEnginePool<ImageInputData, ImageLabelPredictions> predictionEnginePool)
		{
			_CustomVisionKey = configuration["AzureServices:HatDetection:Key"];
			_AzureEndpoint = configuration["AzureServices:HatDetection:CustomVisionEndpoint"];
			_TwitchChannel = configuration["StreamServices:Twitch:Channel"];
			_AzureProjectId = Guid.Parse(configuration["AzureServices:HatDetection:ProjectId"]);
			_TrainHat = service;
			_Repository = repository;
			_HubContext = hubContext;
			_PredictionEnginePool = predictionEnginePool;
			_labelsFilePath = ScreenshotTrainingService.GetAbsolutePath(@"ML\TensorflowModel\labels.txt");

		}

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			if (string.IsNullOrEmpty(IterationName)) {
				await IdentifyIterationName();
			}

			await _HubContext.Clients.All.SendAsync("shutter");
			var obsImage = await _TrainHat.GetScreenshotFromObs();

			////////////////////////////
			//var bestMatch = await PredictWithAzure(chatService, obsImage);
			var bestMatch = PredictWithMLNet(obsImage);

			if (bestMatch == null || bestMatch.Probability < 0.7D) {
				await chatService.SendMessageAsync("csharpAngry 404 Hat Not Found!  Let's ask a moderator to !addhat so we can identify it next time");
				// do we store the image?
				return;
			}

			var hatData = (await _Repository.GetHatData(bestMatch.TagName));
			var nameToReport = (hatData == null ? bestMatch.TagName : (string.IsNullOrEmpty(hatData.Name) ? bestMatch.TagName : hatData.Name));
			await chatService.SendMessageAsync($"csharpClip I think (with {bestMatch.Probability.ToString("0.0%")} certainty) Jeff is currently wearing his {nameToReport} hat csharpClip");
			if (hatData != null && !string.IsNullOrEmpty(hatData.Description)) await chatService.SendMessageAsync(hatData.Description);

			await _HubContext.Clients.All.SendAsync("hatDetected", bestMatch.Probability.ToString("0.0%"), bestMatch.TagName, nameToReport, hatData?.Description);

		}

		private async Task IdentifyIterationName()
		{

			var client = new CustomVisionTrainingClient() { 
				ApiKey = _CustomVisionKey,
				Endpoint = _AzureEndpoint
			};

			var iterations = await client.GetIterationsAsync(_AzureProjectId);
			IterationName = iterations
				.Where(i => !string.IsNullOrEmpty(i.PublishName) && i.Status == "Completed")
				.OrderByDescending(i => i.LastModified).First().PublishName;

		}

		public async Task<PredictMatch> PredictWithAzure(IChatService chatService, Stream obsImage) {

			var client = new CustomVisionPredictionClient()
			{
				ApiKey = _CustomVisionKey,
				Endpoint = _AzureEndpoint,
			};

			ImagePrediction result;
			try
			{
				result = await client.DetectImageWithNoStoreAsync(_AzureProjectId, IterationName, obsImage);
			}
			catch (CustomVisionErrorException ex)
			{



				if (ex.Response.StatusCode == HttpStatusCode.NotFound)
				{
					await IdentifyIterationName();
				}

				return null;

			}

			if (DateTime.UtcNow.Subtract(result.Created).TotalSeconds > Cooldown.Value.TotalSeconds)
			{
				await chatService.SendMessageAsync($"I previously predicted this hat about {DateTime.UtcNow.Subtract(result.Created).TotalSeconds} seconds ago");
			}

			var bestMatch = result.Predictions.OrderByDescending(p => p.Probability).FirstOrDefault();
			return new PredictMatch()
			{
				Probability = bestMatch.Probability,
				TagName = bestMatch.TagName
			};

		}

		public PredictMatch PredictWithMLNet(Stream image) {

			//Convert to Bitmap
			var bitmapImage = (Bitmap)Image.FromStream(image);

			//Set the specific image data into the ImageInputData type used in the DataView
			var imageInputData = new ImageInputData { Image = bitmapImage };

			//Predict code for provided image
			var imageLabelPredictions = _PredictionEnginePool.Predict(imageInputData);

			//Predict the image's label (The one with highest probability)
			var imageBestLabelPrediction = FindBestLabelWithProbability(imageLabelPredictions, imageInputData);

			return new PredictMatch
			{
				Probability = (double)imageBestLabelPrediction.Probability,
				TagName = imageBestLabelPrediction.PredictedLabel
			};

		}

		private ImagePredictedLabelWithProbability FindBestLabelWithProbability(ImageLabelPredictions imageLabelPredictions, ImageInputData imageInputData)
		{
			//Read TF model's labels (labels.txt) to classify the image across those labels
			var labels = ReadLabels(_labelsFilePath);

			var probabilities = imageLabelPredictions.PredictedLabels;

			//Set a single label as predicted or even none if probabilities were lower than 70%
			var imageBestLabelPrediction = new ImagePredictedLabelWithProbability()
			{
				ImageId = imageInputData.GetHashCode().ToString(), //This ID is not really needed, it could come from the application itself, etc.
			};

			(imageBestLabelPrediction.PredictedLabel, imageBestLabelPrediction.Probability) = GetBestLabel(labels, probabilities);

			return imageBestLabelPrediction;
		}

		private (string, float) GetBestLabel(string[] labels, float[] probs)
		{
			var max = probs.Max();
			var index = probs.AsSpan().IndexOf(max);

			if (max > 0.7)
				return (labels[index], max);
			else
				return ("None", max);
		}

		private string[] ReadLabels(string labelsLocation)
		{
			return System.IO.File.ReadAllLines(labelsLocation);
		}


		public class PredictMatch {

			public double Probability { get; set; }

			public string TagName { get; set; }

		}

	}

}
