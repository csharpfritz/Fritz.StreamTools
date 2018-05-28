using System;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class PingCommand : IBasicCommand
	{
		public string Trigger => "ping";

		public string Description => "Receive a quick acknowledgement from the bot through a whisper";

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			await chatService.SendWhisperAsync(userName, "pong");
		}
	}

}
