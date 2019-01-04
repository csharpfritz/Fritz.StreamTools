using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Services;

namespace Fritz.Chatbot.Commands
{
	public class SleepCommand : IBasicCommand2
	{

		internal const string TriggerText = "sleep";

		public string Trigger => TriggerText;

		public string Description => "Allows the broadcaster to put the bot in Sleep mode, which disables all command processing";

		public TimeSpan? Cooldown => TimeSpan.Zero;

		public async Task Execute(IChatService chatService, string userName, bool isModerator, bool isBroadcaster, ReadOnlyMemory<char> rhs)
		{

			if (!isBroadcaster)
			{
				await chatService.SendWhisperAsync(userName, $"This is a broadcaster only command");
				return;
			}

			await Execute(chatService, userName, rhs);

		}

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			FritzBot.IsSleeping = !FritzBot.IsSleeping;
			await chatService.SendWhisperAsync(userName, $"Bot is now set to SleepMode: {FritzBot.IsSleeping}");

		}
	}
}
