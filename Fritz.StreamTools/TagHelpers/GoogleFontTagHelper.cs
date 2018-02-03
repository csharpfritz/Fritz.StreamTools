using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.TagHelpers
{
	[HtmlTargetElement("google-font")]
	public class GoogleFontTagHelper : TagHelper
	{
		public override void Process(TagHelperContext context, TagHelperOutput output)
		{
			var content = Task.Run(function: async ()=> await output.GetChildContentAsync()).Result;
			var fontName = content.GetContent();
			if (string.IsNullOrWhiteSpace(fontName))
				return;

			output.TagName = "link";
			output.TagMode = TagMode.StartTagOnly;
			output.Attributes.Add("rel", "stylesheet");
			output.Attributes.Add("href", $"https://fonts.googleapis.com/css?family={fontName}");
		}
	}
}
