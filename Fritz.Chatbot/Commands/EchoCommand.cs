using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class EchoCommand : ICommand
	{
		public IChatService ChatService { get; set; }

		public string Name => "echo";

		public string Description => "Repeat the text that was requested by the echo command";

		public async Task Execute(string userName, string fullCommandText)
		{

			var segments = fullCommandText.Substring(1).Split(' ');

			if (segments.Length < 2)
				return;
			await ChatService.SendWhisperAsync(userName, "Echo reply: " + string.Join(' ', segments.Skip(1)));

		}
	}
}
