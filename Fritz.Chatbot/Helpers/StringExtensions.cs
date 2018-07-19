using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Fritz.Chatbot.Helpers
{
  public static class StringExtensions
  {
		private readonly static Regex _regex = new Regex(@"\[([\w\s?.-]+)\]\(((?:http:\/\/www\.|https:\/\/www\.|http:\/\/|https:\/\/)?[a-z0-9]+(?:[\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(?:[0-9]{1,5})?(?:\/.*)?)\)");
		public static string HandleMarkdownLinks(this string value)
		{

			foreach (Match match in _regex.Matches(value))
			{

				var title = match.Groups[1]; // The link text (that between the [] in markdown)
				var url = match.Groups[2];   // The url (that between the () in markdown)

				value = value.Replace(match.Value, $"{title} found at {url} ").Replace("  ", " ");

			}

			return value;
		}

		public static bool IsValidRegularExpression(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}

			try
			{
				Regex.IsMatch("", value);
			}
			catch (ArgumentException)
			{
				return false;
			}

			return true;
		}
  }
}
