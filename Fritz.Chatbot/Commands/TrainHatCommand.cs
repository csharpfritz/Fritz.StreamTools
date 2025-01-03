using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Fritz.Chatbot.Commands
{
	public class TrainHatCommand : IBasicCommand2
	{
		private readonly ITrainHat _TrainHat;
		private readonly IHubContext<ObsHub> _HubContext;

		public string Trigger => "trainhat";
		public string Description => "Moderators can capture 15 screenshots in an effort to help train the hat detection AI";
		public TimeSpan? Cooldown => TimeSpan.FromMinutes(15);

		public TrainHatCommand(ScreenshotTrainingService service, IHubContext<ObsHub> hubContext)
		{
			_TrainHat = service;
			_HubContext = hubContext;
		}

		public async Task Execute(IChatService chatService, string userName, bool isModerator, bool isVip, bool isBroadcaster, ReadOnlyMemory<char> rhs)
		{

			if (!(isModerator || isBroadcaster)) return;

			int.TryParse(rhs.ToString(), out var picCount);
			picCount = (picCount < 1) ? ScreenshotTrainingService.DefaultTrainingCount : picCount;
			_TrainHat.StartTraining(picCount);
			await _HubContext.Clients.All.SendAsync("shutter");
			await chatService.SendMessageAsync($"Started taking screenshots, 1 every {ScreenshotTrainingService.TrainingIntervalInSeconds} seconds for the next {ScreenshotTrainingService.TrainingIntervalInSeconds * picCount} seconds");

		}

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			throw new NotImplementedException();
		}
	}
}
