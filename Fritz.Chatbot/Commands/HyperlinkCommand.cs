using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class HyperlinkCommand : IExtendedCommand
	{

		// TODO: Do not check for links posted by the bot

		public string Name => "HyperLink";
		public string Description => "";
		public int Order => 2;
		public bool Final => false;

		public TimeSpan? Cooldown => null;

		private const string HttpCheckPattern = @"http(s)?:?";
		private static readonly Regex reCheck = new Regex(HttpCheckPattern, RegexOptions.IgnoreCase);

		public bool CanExecute(string userName, string fullCommandText)
		{

			// Match the regular expression pattern against a text string.
			return !ImageDescriptorCommand._InstagramCheck.IsMatch(fullCommandText) && reCheck.IsMatch(fullCommandText);

		}

		public Task Execute(IChatService chatService, string userName, string fullCommandText)
		{

			// Use HttpClient to request URL

			// Grab HTML title from URL

			// ??Moderate as needed??

			// Output title to ChatService

			return Task.CompletedTask;

		}
	}
}
