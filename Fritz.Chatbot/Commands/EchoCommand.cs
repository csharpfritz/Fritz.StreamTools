using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class EchoCommand : CommandBase
	{
		override public string Name => "echo";

		override public string Description => "Repeat the text that was requested by the echo command";

		override public async Task Execute(IChatService chatService, string userName, string fullCommandText)
		{

			var segments = fullCommandText.Substring(1).Split(' ');

			if (segments.Length < 2)
				return;
			await chatService.SendWhisperAsync(userName, "Echo reply: " + string.Join(' ', segments.Skip(1)));

		}
	}
}
