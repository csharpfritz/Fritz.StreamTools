using Fritz.Chatbot.Commands;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Test.Chatbot
{

	public class ShoutoutCommandFixture
	{
		private readonly MockRepository _Mockery;
		private readonly Mock<IHttpClientFactory> _ClientFactory;
		private readonly Mock<IChatService> _ChatService;
		private readonly ILogger _Logger;

		public ShoutoutCommandFixture(ITestOutputHelper output)
		{
			_Mockery = new MockRepository(MockBehavior.Default);
			_ClientFactory = new Mock<IHttpClientFactory>();
			_ChatService = new Mock<IChatService>();
			_Logger = new XunitLogger<ShoutoutCommand>(output);

		}


		[Fact]
		public async Task ShouldShoutoutCsharpfritz() {

			// arrange
			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("client-id", "t7y5txan5q662t7zj7p3l4wlth8zhv");
			_ClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
				.Returns(client);
			var sut = new ShoutoutCommand(_ClientFactory.Object, _Logger);
			_ChatService.Setup(c => c.SendMessageAsync(It.Is<string>(s => s.StartsWith("Please follow @csharpfritz")))).Verifiable();

			// act
			await sut.Execute(_ChatService.Object, "csharpfritz", false, false, true, "csharpfritz".ToCharArray());

			// assert
			_ChatService.Verify();


		}

	}


}
