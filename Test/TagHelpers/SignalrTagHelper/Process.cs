using System.Linq;
using Fritz.StreamTools.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Xunit;

namespace Test.TagHelpers.SignalrTagHelper
{

	public class Process
	{

		[Theory]
		[AutoMoqData]
		public void ReturnsScriptTag(SignalrTagHelperOptions options, TagHelperContext context, TagHelperOutput output)
		{

			// arrange
			var sut = new Fritz.StreamTools.TagHelpers.SignalrTagHelper(options);

			// act
			sut.Process(context, output);

			// assert
			Assert.Equal("script", output.TagName);
			Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
			Assert.Single(output.Attributes);
			Assert.Equal("src", output.Attributes.First().Name);
			Assert.Equal(options.ClientLibarySource, output.Attributes.First().Value);

		}

	}

}
