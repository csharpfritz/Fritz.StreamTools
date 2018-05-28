using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class GitHubCommand : IBasicCommand
	{
		public string Trigger => "github";

		public string Description => "Outputs the URL of Jeff's Github Repository";

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			await chatService.SendMessageAsync("Jeff's Github repository can by found here: https://github.com/csharpfritz/");
		}
	}
}
