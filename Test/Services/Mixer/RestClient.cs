using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using Fritz.StreamTools.Helpers;
using Fritz.StreamTools.Services.Mixer;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Test.Services.Mixer
{
	public partial class RestClient : Base
	{
		private const int ChannelId = 3466523;
		private const string ChannelName = "Test1";
		private const int UserId = 43564326;
		private const string UserName = "TestUserA";
		private const int OtherUserId = 2345;
		private const string OtherUserName = "OtherUser";
		private readonly string[] Endpoints;
		private readonly DateTime NowTestValue;

		private readonly DateTime StartedAtTestValue;

		public SimulatedHttpMessageHandler Handler { get; }
		public HttpClient Client { get; }

		public RestClient()
		{
			Endpoints = new string[] { "a", "b", "c", "d" };
			NowTestValue = DateTime.UtcNow;
			StartedAtTestValue = DateTime.UtcNow.AddHours(-1);

			Handler = new SimulatedHttpMessageHandler("/api/v1/");
			Client = new HttpClient(Handler);

			AddDefaultTriggers();
		}

		private void AddDefaultTriggers()
		{
			Handler.On($"channels/{WebUtility.UrlEncode(ChannelName)}", _ => {
				return new JsonContent(new ChannelInfo { Id = ChannelId, UserId = 0 /* not our channel */, NumberOfFollowers = 543, NumberOfViewers = 32 });
			});
			Handler.On($"channels/{WebUtility.UrlEncode(OtherUserName)}", _ => {
				return new JsonContent(new { id = OtherUserId });
			});
			Handler.On($"users/current", _ => new JsonContent(new { id = UserId, username = UserName }));
			Handler.On(new HttpMethod("PATCH"), $"channels/{ChannelId}/users/{OtherUserId}", ctx => {
				if(string.IsNullOrEmpty(ctx.Content)) throw new HttpRequestException("Empty content");
				var doc = JToken.Parse(ctx.Content);
				if(doc["add"] == null && doc["remove"] == null)	throw new HttpRequestException("need add or remove in content");
				var e = doc["add"] ?? doc["remove"];
				if(!e.Values<string>().Contains("Banned")) throw new HttpRequestException("[Banned] as arg to add/remove");
				return new JsonContent(new { });
			});
			Handler.On($"chats/{ChannelId}", _ => {
				return new JsonContent(new { authkey = SimAuth.Value.ChatAuthKey, Endpoints });
			});
			Handler.On($"channels/{ChannelId}/manifest.light2", _ => {
				return new JsonContent(new {
					// SO 8601 date time format
					now = NowTestValue.ToString("o"),
					startedAt = StartedAtTestValue.ToString("o")
				});
			});
		}

		[Fact]
		public void CheckAuthorizationHeader()
		{
			var sut = new MixerRestClient(LoggerFactory, Client, ChannelName, Token);
			sut.GetChannelInfoAsync().Wait();

			// Assert
			var req = Handler.FindRequest($"channels/{WebUtility.UrlEncode(ChannelName)}");
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
			var req = Handler.FindRequest($"channels/{WebUtility.UrlEncode(ChannelName)}");
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
			var id1 = sut.GetChannelIdAsync().Result;
			var id2 = sut.GetChannelIdAsync().Result;

			// Assert
			Handler.RequestHistory.Count.Should().Be(1);
			id1.Should().Be(ChannelId);
			id2.Should().Be(ChannelId);
		}

		[Fact]
		public void ParsesStreamStartAndRoundsCorrectly()
		{
			var sut = new MixerRestClient(LoggerFactory, Client, ChannelName, Token);
			sut.GetChannelInfoAsync().Wait(); // We need this first
			var r = sut.GetStreamStartedAtAsync().Result;

			// Assert
			r.Should().NotBeNull();
			r.Value.ToUnixTimeSeconds().Should().Be(new DateTimeOffset(StartedAtTestValue).ToUnixTimeSeconds()); // Check only seconds difference
		}

		[Fact]
		public void CanLookupUserIdAsync()
		{
			var sut = new MixerRestClient(LoggerFactory, Client, ChannelName, Token);
			sut.GetChannelInfoAsync().Wait(); // We need this first
			var r = sut.LookupUserIdAsync(OtherUserName).Result;

			// Assert
			r.Should().Be(OtherUserId);
		}
	}
}
