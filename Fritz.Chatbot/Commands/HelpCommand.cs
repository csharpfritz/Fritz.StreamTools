using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fritz.Chatbot.Commands
{
	public class HelpCommand : CommandBase
	{
		private readonly IServiceProvider _serviceProvider;

		public HelpCommand(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		override public string Name => "help";

		override public string Description => "Get information about the functionality available on this channel";

		override public async Task Execute(IChatService chatService, string userName, string fullCommandText)
		{

			var commands = _serviceProvider.GetServices<ICommand>();

			if (fullCommandText == "!help")
			{

				var availableCommands = String.Join(" ", commands.Where(c => !string.IsNullOrEmpty(c.Name)).Select(c => $"!{c.Name.ToLower()}"));

				await chatService.SendMessageAsync($"Supported commands: {availableCommands}");
				return;
			}

			var commandToHelpWith = fullCommandText.Replace("!help ", "");
			var cmd = commands.FirstOrDefault(c => c.Name.Equals(commandToHelpWith, StringComparison.InvariantCultureIgnoreCase));
			if (cmd == null)
			{
				await chatService.SendMessageAsync("Unknown command to provide help with.");
				return;
			}

			await chatService.SendMessageAsync($"{commandToHelpWith}: {cmd.Description}");

		}

	}

}
