using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class QuotesCommand : ICommand
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

		public IChatService ChatService { get; set; }

		public string Name => "quote";

		public string Description => "Return a random quote to the chat room";

		public async Task Execute(string userName, string fullCommandText)
		{

			if (_quotes == null) return;

			await ChatService.SendMessageAsync(_quotes[_random.Next(_quotes.Length)]);

		}

	}
}
