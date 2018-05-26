using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class PingCommand : ICommand
	{
		public IChatService ChatService { get; set; }

		public string Name => "ping";

		public string Description => "Receive a quick acknowledgement from the bot through a whisper";

    public int Order => 100;

    public bool CanExecute(string userName, string fullCommandText) => true;

    public async Task Execute(string userName, string fullCommandText)
		{
			await ChatService.SendWhisperAsync(userName, "pong");
		}
	}

}
