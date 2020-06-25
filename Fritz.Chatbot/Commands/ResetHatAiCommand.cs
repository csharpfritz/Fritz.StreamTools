using Fritz.StreamLib.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.Chatbot.Commands
{
	public class ResetHatAiCommand : IBasicCommand2
	{

		public string Trigger => "resethatai";
		public string Description => "Reset the Hat detection AI after adding new hats";
		public TimeSpan? Cooldown { get; } = TimeSpan.FromSeconds(30);

		public async Task Execute(IChatService chatService, string userName, bool isModerator, bool isVip, bool isBroadcaster, ReadOnlyMemory<char> rhs)
		{

			if (!(isModerator || isBroadcaster)) return;

			PredictHatCommand.IterationName = string.Empty;
			await chatService.SendMessageAsync("Reset the AI iteration and will detect the latest for Hat identification next time !hat is called");

		}

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			return Task.CompletedTask;
		}
	}
}
