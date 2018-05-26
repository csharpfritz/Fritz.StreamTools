
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
  public class HyperlinkCommand : ICommand
  {

		private const string HttpCheckPattern = @"http(s)?:?";
		private static readonly Regex reCheck = new Regex(HttpCheckPattern, RegexOptions.IgnoreCase);

    public IChatService ChatService { get; set; }

    public string Name => "";

    public string Description => "";

    public int Order => 5;

    public bool CanExecute(string userName, string fullCommandText)
    {

			// Match the regular expression pattern against a text string.
			return reCheck.IsMatch(fullCommandText);

    }

    public Task Execute(string userName, string fullCommandText)
    {

		 	// Use HttpClient to request URL

		 	// Grab HTML title from URL

		 	// ??Moderate as needed??

		 	// Output title to ChatService

		 return Task.CompletedTask;

    }
  }

}
