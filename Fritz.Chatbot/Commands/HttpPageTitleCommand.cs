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
  class HttpPageTitleCommand : ICommand
  {
		private const string DEFAULT_URL_REGEX = "(https?:\\/\\/)?(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{2,256}\\.[a-z]{2,6}\\b([-a-zA-Z0-9@:%_\\+.~#?&//=]*)";
		private const string DEFAULT_TITLE_REGEX = "<title>\\s*(.+?)\\s*<\\/title>";
		private const string DEFAULT_LINK_MESSAGE_TEMPLATE = "{{username}}'s linked page title: {{title}}";

		public HttpPageTitleCommand(string urlRegex, string titleRegex, string messageTemplate)
		{
			UrlRegex = !string.IsNullOrEmpty(urlRegex) ? urlRegex : DEFAULT_URL_REGEX;
			TitleRegex = !string.IsNullOrEmpty(titleRegex) ? titleRegex : DEFAULT_TITLE_REGEX;
			MessageTemplate = !string.IsNullOrEmpty(messageTemplate) ? messageTemplate : DEFAULT_LINK_MESSAGE_TEMPLATE;
		}

		private readonly string UrlRegex;

		private readonly string TitleRegex;

		private readonly string MessageTemplate;

		public IChatService ChatService { get; set; }

		public string Name => "PageTitle";

		public string Description => "Write linked page title to chat";

		public bool IsInternal => true;

		public async Task Execute(string userName, string fullCommandText)
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
			return Regex.Matches(fullCommandText, UrlRegex, RegexOptions.IgnoreCase).Select(m => m.Value);
		}

		private string GetSource(string url)
		{
			var uri = new UriBuilder(url).Uri;
			var source = new WebClient().DownloadString(uri);
			return source;
		}

		private string GetTitle(string source)
		{
			var match = Regex.Match(source, TitleRegex, RegexOptions.IgnoreCase);
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
			return MessageTemplate.Replace("{{username}}", username).Replace("{{title}}", title);
		}

		public static bool ContainsLink(string urlRegex, string message)
		{
			var internalRegex = urlRegex;
			if (string.IsNullOrEmpty(urlRegex))
			{
				internalRegex = DEFAULT_URL_REGEX;
			}

			return Regex.IsMatch(message, internalRegex, RegexOptions.IgnoreCase);
		}
	}
}
