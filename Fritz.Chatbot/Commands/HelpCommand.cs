using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Services;

namespace Fritz.Chatbot.Commands
{
	public class HelpCommand : ICommand
	{
		public IChatService ChatService { get; set; }

		public string Name => "help";

		public string Description => "Get information about the functionality available on this channel";

		public async Task Execute(string userName, string fullCommandText)
		{

			if (fullCommandText == "!help")
			{

				var availableCommands = String.Join(" ", FritzBot._CommandRegistry.Select((k) => $"!{k.Key}"));

				await ChatService.SendMessageAsync($"Supported commands: {availableCommands}");
				return;
			}

			var commandToHelpWith = fullCommandText.Replace("!help ", "");
			var cmd = FritzBot._CommandRegistry[commandToHelpWith.ToLowerInvariant()];
			if (cmd == null)
			{
				await ChatService.SendMessageAsync("Unknown command to provide help with.");
				return;
			}

			await ChatService.SendMessageAsync($"{commandToHelpWith}: {cmd.Description}");

		}

	}

}
