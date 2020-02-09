using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fritz.Chatbot.Commands
{


	/// <summary>
	/// This is a command about showing the title of a page references by the <a href="https://github.com/csharpfritz">ChatBot</a>
	/// 
	/// </summary>
	class HttpPageTitleCommand : IExtendedCommand
	{
		private static readonly Regex UrlRegex = new Regex("(https?:\\/\\/)?(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{2,256}\\.[a-z]{2,6}\\b([-a-zA-Z0-9@:%_\\+.~#?&//=]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex TitleRegex = new Regex("<title>\\s*(.+?)\\s*<\\/title>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private const string LINK_MESSAGE_TEMPLATE = "{{username}}'s linked page title: {{title}}";

		private static HttpPageTitleCommand cmd;

		public IChatService ChatService { get; private set; }

		public string Name => "PageTitle";

		public string Description => "Write linked page title to chat";

		public bool IsInternal => true;

		public int Order => 20;

		public bool Final => false;

		public TimeSpan? Cooldown => null;

		private async Task Execute(string userName, string fullCommandText)
		{

			var urls = GetUrls(fullCommandText);
			if (urls == null || !urls.Any())
			{
				return;
			}

			foreach (var url in urls)
			{
				var source = GetSource(url);
				if (string.IsNullOrEmpty(source))
				{
					continue;
				}

				var title = GetTitle(source);
				if (string.IsNullOrEmpty(title))
				{
					continue;
				}

				var message = GetMessageFromTemplate(userName, title);
				await ChatService.SendMessageAsync(message);
			}

			return;
		}

		private IEnumerable<string> GetUrls(string fullCommandText)
		{
			return UrlRegex.Matches(fullCommandText).Select(m => m.Value);
		}

		private string GetSource(string url)
		{
			var uri = new UriBuilder(url).Uri;
			var source = new WebClient().DownloadString(uri);
			return source;
		}

		private string GetTitle(string source)
		{
			var match = TitleRegex.Match(source);
			if (!match.Success)
			{
				return null;
			}

			var titleStart = match.Value.IndexOf('>') + 1;
			var titleLength = match.Value.LastIndexOf('<') - titleStart;
			var title = match.Value.Substring(titleStart, titleLength);
			return title;
		}

		private string GetMessageFromTemplate(string username, string title)
		{
			return LINK_MESSAGE_TEMPLATE.Replace("{{username}}", username).Replace("{{title}}", title);
		}

		public static bool ContainsLink(string message)
		{
			return UrlRegex.IsMatch(message);
		}

		public bool CanExecute(string userName, string fullCommandText)
		{

			if (FritzBot._OtherBots.Contains(userName.ToLowerInvariant())) return false;

			return HttpPageTitleCommand.ContainsLink(fullCommandText);
		}

		public Task Execute(IChatService chatService, string userName, string fullCommandText)
		{
			this.ChatService = chatService;
			return Execute(userName, fullCommandText);
		}
	}
}
