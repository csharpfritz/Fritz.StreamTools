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
		private const int OtherUserId = 2345;
		private const string OtherUserName = "OtherUser";
		private readonly string[] Endpoints;


		public SimulatedHttpMessageHandler Handler { get; }
		public HttpClient Client { get; }

		public RestClient()
		{
			Endpoints = new string[] { "a", "b", "c", "d" };

			Handler = new SimulatedHttpMessageHandler();
			Client = new HttpClient(Handler);

			AddDefaultTriggers();
		}

		private void AddDefaultTriggers()
		{
			Handler.AddTrigger(HttpMethod.Get, $"/api/v1/channels/{WebUtility.UrlEncode(ChannelName)}", _ => {
				return new JsonContent(new ChannelInfo { Id = ChannelId, UserId = 0 /* not our channel */, NumberOfFollowers = 543, NumberOfViewers = 32 });
			});
			Handler.AddTrigger(HttpMethod.Get, $"/api/v1/channels/{WebUtility.UrlEncode(OtherUserName)}", _ => {
				return new JsonContent(new { id = OtherUserId });
			});
			Handler.AddTrigger(HttpMethod.Get, $"/api/v1/users/current", _ => new JsonContent(new { id = UserId, username = UserName }));
			Handler.AddTrigger(new HttpMethod("PATCH"), $"/api/v1/channels/{ChannelId}/users/{OtherUserId}", _ => {
				return new JsonContent(new { });
			});
			Handler.AddTrigger(HttpMethod.Get, $"/api/v1/chats/{ChannelId}", _ => {
				return new JsonContent(new { authkey = SimAuth.Value.ChatAuthKey, Endpoints });
			});
		}

		[Fact]
		public void CheckAuthorizationHeader()
		{
			var sut = new MixerRestClient(LoggerFactory, Client, ChannelName, Token);
			sut.GetChannelInfoAsync().Wait();

			// Assert
			var req = Handler.FindRequest($"/api/v1/channels/{WebUtility.UrlEncode(ChannelName)}");
			req.Should().NotBeNull();
			req.Method.Should().Be(HttpMethod.Get);
			req.Headers.Should().Contain(new KeyValuePair<string, string>("Authorization", $"Bearer {Token}"));
		}

		[Fact]
		public void CheckAcceptsHeader()
		{
			var sut = new MixerRestClient(LoggerFactory, Client, ChannelName, Token);
			sut.GetChannelInfoAsync().Wait();

			// Assert
			var req = Handler.FindRequest($"/api/v1/channels/{WebUtility.UrlEncode(ChannelName)}");
			req.Should().NotBeNull();
			req.Method.Should().Be(HttpMethod.Get);
			req.Headers.Should().Contain(new KeyValuePair<string, string>("Accept", "application/json"));
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

		[Fact]
		public void CanBanUser()
		{
			var sut = new MixerRestClient(LoggerFactory, Client, ChannelName, Token);
			sut.GetChannelInfoAsync().Wait(); // We need this first
			var result = sut.BanUserAsync(OtherUserName).Result;

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void CanUnbanUser()
		{
			var sut = new MixerRestClient(LoggerFactory, Client, ChannelName, Token);
			sut.GetChannelInfoAsync().Wait(); // We need this first
			var result = sut.UnbanUserAsync(OtherUserName).Result;

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void ChatAuthRequest()
		{
			var sut = new MixerRestClient(LoggerFactory, Client, ChannelName, Token);
			sut.GetChannelInfoAsync().Wait(); // We need this first
			var result = sut.GetChatAuthKeyAndEndpointsAsync().Result;

			// Assert
			result.Should().NotBeNull();
			result.AuthKey.Should().Be(SimAuth.Value.ChatAuthKey);
			result.Endpoints.Should().BeEquivalentTo(Endpoints);
		}

		[Fact]
		public void CachesChannelId()
		{
			var sut = new MixerRestClient(LoggerFactory, Client, ChannelName, Token);
			var id = sut.GetChannelIdAsync().Result;
			id = sut.GetChannelIdAsync().Result;

			// Assert
			id.Should().Be(ChannelId);
			Handler.RequestHistory.Count.Should().Be(1);
		}
	}
}
