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

		private readonly ITrainHat _TrainHat = ScreenshotTrainingService.Instance;

		public string Trigger => "addhat";
		public string Description => "Moderators can add a screenshot of the stream to the image detection library";
		public TimeSpan? Cooldown => TimeSpan.FromMinutes(2);

		public async Task Execute(IChatService chatService, string userName, bool isModerator, bool isVip, bool isBroadcaster, ReadOnlyMemory<char> rhs)
		{
			if (!(isModerator || isBroadcaster || isVip)) return;
			await _TrainHat.AddScreenshot();
			await chatService.SendMessageAsync("csharpCat Taking a screenshot and adding image to the knowledgebase");

		}

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			throw new NotImplementedException();
		}
	}
}
