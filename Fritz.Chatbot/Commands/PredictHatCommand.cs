using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

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
		//private readonly HatDescriptionRepository _Repository;
		private readonly IHubContext<ObsHub> _HubContext;

		public PredictHatCommand(IConfiguration configuration, ScreenshotTrainingService service, IHubContext<ObsHub> hubContext)
		{
			_CustomVisionKey = configuration["AzureServices:HatDetection:Key"];
			_AzureEndpoint = configuration["AzureServices:HatDetection:CustomVisionEndpoint"];
			_TwitchChannel = configuration["StreamServices:Twitch:Channel"];
			_AzureProjectId = Guid.Parse(configuration["AzureServices:HatDetection:ProjectId"]);
			_TrainHat = service;
			//_Repository = repository;
			_HubContext = hubContext;
		}

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			var client = new CustomVisionPredictionClient()
			{
				ApiKey = _CustomVisionKey,
				Endpoint = _AzureEndpoint,
			};

			await _HubContext.Clients.All.SendAsync("shutter");
			var obsImage = await _TrainHat.GetScreenshotFromObs();

			////////////////////////////


		}

	}

	internal record HatData(string Name, string Description);

}
