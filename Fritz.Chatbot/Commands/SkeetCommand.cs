using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{

	public class SkeetCommand : IBasicCommand
	{
		const string QUOTES_FILENAME = "SkeetQuotes.txt";
		internal string[] _quotes;
		private readonly Random _random = new Random();

		public SkeetCommand()
		{
			if (File.Exists(QUOTES_FILENAME))
			{
				_quotes = File.ReadLines(QUOTES_FILENAME).ToArray();
			}
		}

		public SkeetCommand(string[] quotes)
		{
			_quotes = quotes;
		}

		public string Trigger => "skeet";
		public string Description => "Return a random quote about Jon Skeet to the chat room";

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			if (_quotes == null) return;
			await chatService.SendMessageAsync(_quotes[_random.Next(_quotes.Length)]);
		}
	}

}
