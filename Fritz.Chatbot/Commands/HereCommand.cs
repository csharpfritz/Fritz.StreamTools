using Fritz.StreamLib.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.Chatbot.Commands
{
	public class HereCommand : IBasicCommand
	{
		public string Trigger { get; } = "here";
		public string Description { get; } = "A do-nothing command that will not crash the bot when we run a giveaway";
		public TimeSpan? Cooldown { get; } = null;

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			// do nothing
			return Task.CompletedTask;
		}
	}
}
