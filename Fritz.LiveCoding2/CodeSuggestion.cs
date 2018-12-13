using Newtonsoft.Json.Linq;

namespace Fritz.LiveCoding2
{
	public class CodeSuggestion
	{

		public string UserName { get; set; }

		public string FileName { get; set; }

		public int LineNumber { get; set; }

		public string Body { get; set; }

	}
}

