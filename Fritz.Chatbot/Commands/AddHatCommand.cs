using Fritz.StreamLib.Core;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace Fritz.Chatbot.Commands
{
	public class AddHatCommand : IBasicCommand2
	{
		public string Trigger => "addhat";
		public string Description => "Moderators can add a screenshot of the stream to the image detection library";
		public TimeSpan? Cooldown => TimeSpan.FromMinutes(2);

		private string _CustomVisionKey = "";
		private string _AzureEndpoint = "";
		private string _TwitchChannel = "";
		private Guid _AzureProjectId;

		public AddHatCommand(IConfiguration configuration)
		{
			_CustomVisionKey = configuration["AzureServices:HatDetection:Key"];
			_AzureEndpoint = configuration["AzureServices:HatDetection:CustomVisionEndpoint"];
			_TwitchChannel = configuration["StreamServices:Twitch:Channel"];
			_AzureProjectId = Guid.Parse(configuration["AzureServices:HatDetection:ProjectId"]);
		}

		public string TwitchScreenshotUrl => $"https://static-cdn.jtvnw.net/previews-ttv/live_user_{_TwitchChannel}-1280x720.jpg?_=";

		public async Task Execute(IChatService chatService, string userName, bool isModerator, bool isVip, bool isBroadcaster, ReadOnlyMemory<char> rhs)
		{
			if (!(isModerator || isBroadcaster || isVip)) return;
			var trainingClient = new CustomVisionTrainingClient()
			{
                ApiKey = _CustomVisionKey,
                Endpoint = _AzureEndpoint
			};

			await trainingClient.CreateImagesFromUrlsAsync(_AzureProjectId, new ImageUrlCreateBatch(
				new List<ImageUrlCreateEntry> {
					new ImageUrlCreateEntry(TwitchScreenshotUrl + Guid.NewGuid().ToString())
				}
			));

		}

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			throw new NotImplementedException();
		}
	}
}
