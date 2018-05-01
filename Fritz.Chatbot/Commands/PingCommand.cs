using System;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class PingCommand : ICommand
	{
		public IChatService ChatService { get; set; }

		public string Name => "ping";

		public string Description => "Receive a quick acknowledgement from the bot through a whisper";

		public async Task Execute(string userName, string fullCommandText)
		{
			var thisTask = ChatService.SendWhisperAsync(userName, "pong");
			
			try {
				await thisTask;
			} catch (Exception ex) { // the first of the InnerExcpetion
				var howManyExceptions = thisTask.Exception.InnerExceptions[0];
			}


		}
	}

}
