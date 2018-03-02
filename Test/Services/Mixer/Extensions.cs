using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Test.Services.Mixer
{
	public static class Extensions
	{
		public static string RemoveWhitespace(this string input)
		{
			return new string(input.ToCharArray()
					.Where(c => !Char.IsWhiteSpace(c))
					.ToArray());
		}

		public static IDictionary<string, string> ToDictionary(this NameValueCollection source)
		{
			return source.Cast<string>().Select(s => new { Key = s, Value = source[s] }).ToDictionary(p => p.Key, p => p.Value);
		}
	}
}
