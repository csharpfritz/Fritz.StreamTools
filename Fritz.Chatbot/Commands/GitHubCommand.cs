using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class GitHubCommand : CommandBase
	{
		override public string Name => "github";

		override public string Description => "Outputs the URL of Jeff's Github Repository";

		override public int Order => 100;

		override public async Task Execute(IChatService chatService, string userName, string fullCommandText)
		{
			await chatService.SendMessageAsync("Jeff's Github repository can by found here: https://github.com/csharpfritz/");
		}
	}
}
