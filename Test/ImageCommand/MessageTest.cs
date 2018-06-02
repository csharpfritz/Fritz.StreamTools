using Fritz.Chatbot.Commands;
using Fritz.StreamLib.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Test.ImageCommand
{
	public class MessageTest
	{

		private readonly MockRepository _Mockery;
		private const string url = "https://sites.google.com/site/commodoren25semiacousticguitar/_/rsrc/1436296758012/basschat/Commodore%20Semi%20-%20Acoustic%20Bass%20Guitar%20%28006%29.jpg";

		public ITestOutputHelper Output { get; }

		public MessageTest(ITestOutputHelper outputHelper)
		{
			_Mockery = new MockRepository(MockBehavior.Loose);
			this.Output = outputHelper;
		}


		[Fact]
		public void Contains_URL_With_Image_Extension()
		{
			var pattern = @"http(s)?:?(\/\/[^""']*\.(?:png|jpg|jpeg|gif))";
	
			var message = $"this is the photo {url}";

			// Instantiate the regular expression object.
			var r = new Regex(pattern, RegexOptions.IgnoreCase);

			// Match the regular expression pattern against a text string.
			var m = r.Match(message);

			Assert.True(m.Captures.Count > 0);
			Assert.Equal(url, m.Captures[0].Value);

		}

		[Fact(Skip ="Azure test")]
		public async void Identifies_Guitar()
		{

			// Arrange
			string outDescription = "";
			var chatService = _Mockery.Create<IChatService>();
			chatService
				.Setup(c => c.SendMessageAsync(It.IsAny<string>()))
				.Callback<string>(msg =>
				{
					outDescription = msg;
					Output.WriteLine(msg);
				})
				.Returns(Task.FromResult(true));

			var sut = new ImageDescriptorCommand("testlocation", "testkey");
			sut.ChatService = chatService.Object;

			// Act
			await sut.Execute("test", url);

			// Assert
			Assert.Contains("guitar", outDescription);

		}


	}
}
