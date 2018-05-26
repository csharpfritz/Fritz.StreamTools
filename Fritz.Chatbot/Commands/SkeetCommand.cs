using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{

  public class SkeetCommand : ICommand
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
	public IChatService ChatService { get; set; }
	public string Name => "skeet";
	public string Description => "Return a random quote about Jon Skeet to the chat room";

    public int Order => 100;

    public async Task Execute(string userName, string fullCommandText)
	{
	  if (_quotes == null) return;
	  await ChatService.SendMessageAsync(_quotes[_random.Next(_quotes.Length)]);
	}

    public bool CanExecute(string userName, string fullCommandText) => true;

  }

}
