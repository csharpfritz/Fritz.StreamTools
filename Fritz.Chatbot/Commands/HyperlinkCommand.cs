using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class HyperlinkCommand : CommandBase
	{

		private const string HttpCheckPattern = @"http(s)?:?";
		private static readonly Regex reCheck = new Regex(HttpCheckPattern, RegexOptions.IgnoreCase);

		override public string Name => "";

		override public string Description => "";

		override public int Order => 1000;

		override public bool CanExecute(string userName, string fullCommandText)
		{

			// Match the regular expression pattern against a text string.
			return reCheck.IsMatch(fullCommandText);

		}

		override public Task Execute(IChatService chatService, string userName, string fullCommandText)
		{

			// Use HttpClient to request URL

			// Grab HTML title from URL

			// ??Moderate as needed??

			// Output title to ChatService

			return Task.CompletedTask;

		}
	}

}
