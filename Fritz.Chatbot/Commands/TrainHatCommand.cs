using Fritz.StreamLib.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.Chatbot.Commands
{
	public class TrainHatCommand : IBasicCommand2
	{
		private readonly ITrainHat _TrainHat = ScreenshotTrainingService.Instance;

		public string Trigger => "trainhat";
		public string Description => "Moderators can capture 15 screenshots in an effort to help train the hat detection AI";
		public TimeSpan? Cooldown => TimeSpan.FromMinutes(15);


		public async Task Execute(IChatService chatService, string userName, bool isModerator, bool isVip, bool isBroadcaster, ReadOnlyMemory<char> rhs)
		{

			if (!(isModerator || isBroadcaster)) return;

			_TrainHat.StartTraining();
			await chatService.SendMessageAsync("Started taking screenshots, 1 per minute for the next 15 minutes");

		}

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			throw new NotImplementedException();
		}
	}
}
