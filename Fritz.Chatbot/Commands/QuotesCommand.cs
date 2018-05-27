using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class QuotesCommand : CommandBase
	{

		const string QUOTES_FILENAME = "SampleQuotes.txt";
		internal string[] _quotes;
		private readonly Random _random = new Random();

		public QuotesCommand()
		{

			if (File.Exists(QUOTES_FILENAME))
			{
				_quotes = File.ReadLines(QUOTES_FILENAME).ToArray();
			}

		}

		override public string Name => "quote";

		override public string Description => "Return a random quote to the chat room";

		override public async Task Execute(IChatService chatService, string userName, string fullCommandText)
		{

			if (_quotes == null) return;

			await chatService.SendMessageAsync(_quotes[_random.Next(_quotes.Length)]);

		}

	}
}
