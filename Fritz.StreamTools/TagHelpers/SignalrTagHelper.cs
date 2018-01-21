using Fritz.StreamTools.StartupServices;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Fritz.StreamTools.TagHelpers
{

	//[HtmlTargetElement("signalr")]
	public class SignalrTagHelper : TagHelper
	{

		private readonly SignalrTagHelperOptions Options;

		public SignalrTagHelper(SignalrTagHelperOptions options)
		{
			Options = options;
		}

		public override void Process(TagHelperContext context, TagHelperOutput output)
		{

			output.TagName = "script";
			output.TagMode = TagMode.StartTagAndEndTag;
			output.Attributes.Add("src", Options.ClientLibarySource);

		}

	}

}