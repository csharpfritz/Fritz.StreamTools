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

		public PredictHatCommand(IConfiguration configuration, ScreenshotTrainingService service, HatDescriptionRepository repository, IHubContext<ObsHub> hubContext)
		{
			_CustomVisionKey = configuration["AzureServices:HatDetection:Key"];
			_AzureEndpoint = configuration["AzureServices:HatDetection:CustomVisionEndpoint"];
			_TwitchChannel = configuration["StreamServices:Twitch:Channel"];
			_AzureProjectId = Guid.Parse(configuration["AzureServices:HatDetection:ProjectId"]);
			_TrainHat = service;
			_Repository = repository;
			_HubContext = hubContext;
		}

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			if (string.IsNullOrEmpty(IterationName)) {
				await IdentifyIterationName();
			}

			var client = new CustomVisionPredictionClient()
			{
				ApiKey = _CustomVisionKey,
				Endpoint = _AzureEndpoint,
			};

			await _HubContext.Clients.All.SendAsync("shutter");
			var obsImage = await _TrainHat.GetScreenshotFromObs();

			////////////////////////////

			ImagePrediction result;
			try
			{
				result = await client.DetectImageWithNoStoreAsync(_AzureProjectId, IterationName, obsImage);
			} catch (CustomVisionErrorException ex) {

				

				if (ex.Response.StatusCode == HttpStatusCode.NotFound) {
					await IdentifyIterationName();
				}

				await chatService.SendMessageAsync("Unable to detect Fritz's hat right now... please try again in 1 minute");
				return;

			}

			if (DateTime.UtcNow.Subtract(result.Created).TotalSeconds > Cooldown.Value.TotalSeconds) {
				await chatService.SendMessageAsync($"I previously predicted this hat about {DateTime.UtcNow.Subtract(result.Created).TotalSeconds} seconds ago");
			}

			var bestMatch = result.Predictions.OrderByDescending(p => p.Probability).FirstOrDefault();
			if (bestMatch.Probability < 0.7D && bestMatch.Probability > 0.5d) {

				if (result.Predictions.Count(b => b.Probability > 0.4d) > 1) {

					var guess1Data = (await _Repository.GetHatData(bestMatch.TagName));
					var guess2Data = (await _Repository.GetHatData(result.Predictions.OrderByDescending(p => p.Probability).Skip(1).First().TagName));
					await chatService.SendMessageAsync($"csharpGuess I'm not quite sure if this is {guess1Data.Name} ({bestMatch.Probability.ToString("0.0%")}) or {guess2Data.Name} ({result.Predictions.OrderByDescending(p => p.Probability).Skip(1).First().Probability.ToString("0.0%")})");
					return;

				} else {
					var guessData = (await _Repository.GetHatData(bestMatch.TagName));
					await chatService.SendMessageAsync($"csharpGuess I'm not quite sure if this is {guessData.Name} ({bestMatch.Probability.ToString("0.0%")})");
					return;
				}

			} else if ((bestMatch?.Probability ?? 0) <= 0.4d)  {
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
	}
}
