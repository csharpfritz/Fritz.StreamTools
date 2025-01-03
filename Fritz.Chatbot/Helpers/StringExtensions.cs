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

			value = Markdig.Markdown.ToPlainText(value);

			value = value.Replace("\n", " ");

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
