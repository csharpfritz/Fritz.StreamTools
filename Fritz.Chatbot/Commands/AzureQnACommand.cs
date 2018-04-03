using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{

	public class AzureQnACommand : ICommand
	{
		public IChatService ChatService { get; set; }

		public string Name => "qna";

		public string Description => "Answer questions using Azure Cognitive Services and Jeff's FAQ on the LiveStream wiki";

		public Task Execute(string userName, string fullCommandText)
		{
			throw new NotImplementedException();
		}
	}

}
