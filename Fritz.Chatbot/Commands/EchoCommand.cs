using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class EchoCommand : IBasicCommand
  {
		public string Trigger => "echo";

		public string Description => "Repeat the text that was requested by the echo command";

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			if (rhs.IsEmpty)
				return;
			await chatService.SendWhisperAsync(userName, "Echo reply: " + rhs);

		}
	}
}
