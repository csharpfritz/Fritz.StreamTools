using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using Fritz.StreamTools.Helpers;
using Fritz.StreamTools.Services.Mixer;
using Xunit;

namespace Test.Services.Mixer
{
	public partial class RestClient : Base
	{

		private const int ChannelId = 3466523;
		private const string ChannelName = "Test1";
		private const int UserId = 43564326;
		private const string UserName = "TestUserA";
		private const string Token = "123456789abcdefg";

		public SimulatedHttpMessageHandler Handler { get; }
		public HttpClient Client { get; }

		public RestClient()
		{
			Handler = new SimulatedHttpMessageHandler();
			Client = new HttpClient(Handler);

			AddDefaultTriggers();
		}

		private void AddDefaultTriggers()
		{
			Handler.AddTrigger(HttpMethod.Get, $"/api/v1/channels/{WebUtility.UrlEncode(ChannelName)}", _ => {
				return new JsonContent(new ChannelInfo { Id = ChannelId, UserId = 0 /* not our channel */, NumberOfFollowers = 543, NumberOfViewers = 32 });
			});
			Handler.AddTrigger(HttpMethod.Get, $"/api/v1/users/current", _ => new JsonContent(new { id = UserId, username = UserName }));
		}

		[Fact]
		public void CheckAuthorizationHeader()
		{
			var sut = new MixerRestClient(LoggerFactory, Client, ChannelName, Token);

			// Act
			sut.GetChannelInfoAsync().Wait();

			// Assert
			var req = Handler.FindRequest($"/api/v1/channels/{WebUtility.UrlEncode(ChannelName)}");
			req.Method.Should().Be(HttpMethod.Get);
			req.Should().NotBeNull();
			req.Headers.Should().Contain(new KeyValuePair<string, string>("Authorization", $"Bearer {Token}"));
		}

		[Fact]
		public void ChannelInfoRequest()
		{
			var sut = new MixerRestClient(LoggerFactory, Client, ChannelName, Token);

			sut.GetChannelInfoAsync().Wait();

			// Assert
			Handler.RequestHistory.Count.Should().Be(2);
			sut.UserId.Should().Be(UserId);
			sut.UserName.Should().Be(UserName);
		}
	}
}
