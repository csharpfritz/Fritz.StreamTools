using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Fritz.Chatbot.Helpers
{
  public static class StringExtensions
  {
		public static string HandleMarkdownLinks(this string value)
		{
			var newValue = value;
			var regex = new Regex(@"\[([\w\s?]+)\]\(((?:http:\/\/www\.|https:\/\/www\.|http:\/\/|https:\/\/)?[a-z0-9]+(?:[\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(?:[0-9]{1,5})?(?:\/.*)?)\)");
			foreach (Match match in regex.Matches(value))
			{
			var title = match.Groups[1]; // The link text (that between the [] in markdown)
			var url = match.Groups[2];   // The url (that between the () in markdown)

			newValue = newValue.Replace(match.Value, $"{title} ({url})");
			}

			return newValue;
	}
  }
}
