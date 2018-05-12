using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Test.ImageCommand
{
    public class MessageTest
    {

        [Fact]
        public void Contains_URL_With_Image_Extension()
        {
            var pattern = @"http(s)?:?(\/\/[^""']*\.(?:png|jpg|jpeg|gif))";
			var url = "https://sites.google.com/site/commodoren25semiacousticguitar/_/rsrc/1436296758012/basschat/Commodore%20Semi%20-%20Acoustic%20Bass%20Guitar%20%28006%29.jpg";

						var message = $"this is the photo {url}";

            // Instantiate the regular expression object.
            Regex r = new Regex(pattern, RegexOptions.IgnoreCase);

            // Match the regular expression pattern against a text string.
            Match m = r.Match(message);

            Assert.True(m.Captures.Count > 0);
						Assert.Equal(url, m.Captures[0].Value);

        }


    }
}
