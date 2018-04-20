using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Fritz.Twitch;
using Xunit;
using Xunit.Abstractions;

namespace Test.Twitch.Proxy
{
	public class GetStreamData
	{
		private static readonly HttpClient _Client;
		private static readonly ConfigurationSettings _Settings;

		static GetStreamData()
		{

			_Client = new HttpClient();
			_Settings = new Fritz.Twitch.ConfigurationSettings
			{
				ChannelName = "csharpfritz",
				ClientId = "t7y5txan5q662t7zj7p3l4wlth8zhv",
				UserId = "96909659"
			};

		}

		public GetStreamData(ITestOutputHelper output)
		{

			this.Output = output;
			this.Logger = new XUnitLogger(Output);

		}

		public ITestOutputHelper Output { get; }
		public XUnitLogger Logger { get; }

		[Fact]
		public void ShouldParseStreamResult()
		{

			var sampleData = @"{
				""data"": [

					{
							""id"": ""26007494656"",
						""user_id"": ""23161357"",
						""game_id"": ""417752"",
						""community_ids"": [
							""5181e78f-2280-42a6-873d-758e25a7c313"",
							""848d95be-90b3-44a5-b143-6e373754c382"",
							""fd0eab99-832a-4d7e-8cc0-04d73deb2e54""
						],
						""type"": ""live"",
						""title"": ""Hey Guys, It's Monday - Twitter: @Lirik"",
						""viewer_count"": 32575,
						""started_at"": ""2017-08-14T16:08:32Z"",
						""language"": ""en"",
						""thumbnail_url"": ""https://static-cdn.jtvnw.net/previews-ttv/live_user_lirik-{width}x{height}.jpg""
					}
				],
				""pagination"": {
					""cursor"": ""eyJiIjpudWxsLCJhIjp7Ik9mZnNldCI6MjB9fQ==""
				}
			}";

			var stream = Fritz.Twitch.Proxy.ParseStreamResult(sampleData);
			Output.WriteLine($"Viewer Count: {stream.ViewerCount}");

			Assert.Equal(32575, stream.ViewerCount);

		}

		[Fact]
		public void ShouldReturnZeroWhenNotStreaming()
		{

			// Arrange
			var sut = new Fritz.Twitch.Proxy(_Client, _Settings, Logger);

			// Act
			var viewerCount = sut.GetViewerCountAsync().GetAwaiter().GetResult();
			Output.WriteLine($"csharpfritz Twitch viewer count: {viewerCount}");

			// Assert
			Assert.Equal(0, viewerCount);

		}


	}
}
