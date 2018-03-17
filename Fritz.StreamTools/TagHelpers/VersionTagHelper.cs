using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.TagHelpers
{
	public class VersionTagHelper : TagHelper
	{

		public override void Process(TagHelperContext context, TagHelperOutput output)
		{

			output.TagName = "span";
			output.Attributes.Add("class", "text-muted");

			var versionInfo = GetType().Assembly.GetName().Version;

			output.Content.Append(versionInfo.ToString());

		}


	}
}
