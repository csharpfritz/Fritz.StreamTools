using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.TagHelpers
{
	[HtmlTargetElement("version")]
	public class VersionTagHelper : TagHelper
	{
		public enum VersionType
		{
			FileVersion = 1,
			ProductVersion = 2,
			InformationalVersion = 2,
			AssemblyVersion = 3
		}

		/// <summary>
		/// Assembly represents the type of the assembly to be versioned.
		/// </summary>
		[HtmlAttributeName("assembly")]
		public System.Type AssemblyType { get; set; }

		/// <summary>
		/// Type represents the type of version to be returned
		/// One of FileVersion, ProductVersion or AssemblyVersion
		/// Defaults to ProductVersion (InformationalVersion)
		/// </summary>
		[HtmlAttributeName("type")]
		public VersionType Type { get; set; } = VersionType.ProductVersion;

		public async override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			var versionString = "";
			output.TagName = "span";
			output.TagMode = TagMode.StartTagAndEndTag;
			var childContent = await output.GetChildContentAsync();
			output.Content.AppendHtml(childContent);
			if (AssemblyType == null)
			{
			AssemblyType = GetType();
			}
			switch (Type)
			{
			case VersionType.FileVersion:
				versionString = AssemblyType
					.GetTypeInfo().Assembly
					.GetCustomAttribute<AssemblyFileVersionAttribute>()?
					.Version ?? "";
				break;
			case VersionType.ProductVersion:    // also covers VersionType.InformationalVersion
				versionString = AssemblyType
					.GetTypeInfo().Assembly
					.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
					.InformationalVersion ?? "";
				break;
			case VersionType.AssemblyVersion:
				versionString = AssemblyType
					.Assembly.GetName().Version.ToString();
				break;
			default:
				break;
			}
			output.Content.Append(versionString);

			await base.ProcessAsync(context, output);
			return;
		}
  }
}
