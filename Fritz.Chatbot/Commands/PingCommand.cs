using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class PingCommand : CommandBase
	{
		override public string Name => "ping";

		override public string Description => "Receive a quick acknowledgement from the bot through a whisper";

		override public async Task Execute(IChatService chatService, string userName, string fullCommandText)
		{
			await chatService.SendWhisperAsync(userName, "pong");
		}
	}

}
