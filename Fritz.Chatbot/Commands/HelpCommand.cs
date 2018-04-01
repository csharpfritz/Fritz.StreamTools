using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class HelpCommand : ICommand
	{
		public IChatService ChatService { get; set; }

		public string Name => "help";

		public string Description => "Get information about the functionality available on this channel";

		public async Task Execute(params string[] args)
		{
			await ChatService.SendMessageAsync("Supported commands: !ping !echo !uptime !quote");
		}
	}

}
