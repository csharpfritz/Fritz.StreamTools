using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using Fritz.StreamTools.Helpers;
using Fritz.StreamTools.Services.Mixer;
using Newtonsoft.Json.Linq;
using Xunit;

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
		private const uint GameTypeId = 6654321;

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
				return new JsonContent(new API.Channel { Id = ChannelId, UserId = 0 /* not our channel */, NumFollowers = 543, ViewersCurrent = 32 });
			});
			Handler.On($"channels/{WebUtility.UrlEncode(OtherUserName)}", _ => {
				return new JsonContent(new { userId = OtherUserId }); // ??? Do I need to return anything else ?
			});
			Handler.On($"channels/{ChannelId}", _ => {
				return new JsonContent(new API.Channel { Id = ChannelId, UserId = UserId, TypeId = GameTypeId, Name = "Test stream title 1", NumFollowers = 543, ViewersCurrent = 32 });
			});
			Handler.On($"users/current", _ => new JsonContent(new { id = UserId, username = UserName }));
			Handler.On(new HttpMethod("PATCH"), $"channels/{ChannelId}/users/{OtherUserId}", _ => new JsonContent(new { }));
			Handler.On($"chats/{ChannelId}", _ => new JsonContent(new { authkey = SimAuth.Value.ChatAuthKey, Endpoints }));
			Handler.On($"channels/{ChannelId}/manifest.light2", _ => {
				return new JsonContent(new {
					// ISO 8601 date time format
					now = NowTestValue.ToString("o"),
					startedAt = StartedAtTestValue.ToString("o")
				});
			});
			Handler.On($"types/{GameTypeId}", _ => new JsonContent(new API.GameTypeSimple { Id = GameTypeId, Name = "TestGameName" }));
			Handler.On($"types", ctx => new JsonContent(new API.GameTypeSimple[] {
				new API.GameTypeSimple { Id = GameTypeId, Name = ctx.Query["query"] }
			}));
			Handler.On(new HttpMethod("PATCH"), $"channels/{ChannelId}", ctx => new JsonContent(new { }));
		}

		[Fact]
		public void CheckAuthorizationHeader()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				// Assert
				var req = Handler.FindRequest($"channels/{WebUtility.UrlEncode(ChannelName)}");
				req.Should().NotBeNull();
				req.Method.Should().Be(HttpMethod.Get);
				req.Headers.Should().Contain(new KeyValuePair<string, string>("Authorization", $"Bearer {Token}"));
			}
		}

		[Fact]
		public void CheckAcceptsHeader()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				// Assert
				var req = Handler.FindRequest($"channels/{WebUtility.UrlEncode(ChannelName)}");
				req.Should().NotBeNull();
				req.Method.Should().Be(HttpMethod.Get);
				req.Headers.Should().Contain(new KeyValuePair<string, string>("Accept", "application/json"));
			}
		}

		[Fact]
		public void CanInit()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				// Assert
				Handler.RequestHistory.Count.Should().Be(2);
				sut.UserId.Should().Be(UserId);
				sut.UserName.Should().Be(UserName);
			}
		}

		[Fact]
		public void WillRetryInit()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client) { RetryDelay = 0, MaxTries = 5 })
			{
				Action call = () => sut.InitAsync("InvalidChannelName", Token).Wait(Simulator.TIMEOUT);

				// Assert
				call.Should().Throw<MixerException>();
				Handler.RequestHistory.Count.Should().Be(5);
			}
		}

		[Fact]
		public void CanBanUser()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				var result = sut.BanUserAsync(OtherUserName).Result;

				// Assert
				var req = Handler.FindRequest($"channels/{ChannelId}/users/{OtherUserId}", new HttpMethod("PATCH"));
				req.Should().NotBeNull();
				req.Content.Should().NotBeNullOrWhiteSpace();

				var doc = JToken.Parse(req.Content);
				doc["add"].Should().NotBeNull();
				doc["add"].Values<string>().Should().Contain("Banned");
			}
		}

		[Fact]
		public void CanUnbanUser()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				var result = sut.UnbanUserAsync(OtherUserName).Result;

				// Assert
				var req = Handler.FindRequest($"channels/{ChannelId}/users/{OtherUserId}", new HttpMethod("PATCH"));
				req.Should().NotBeNull();
				req.Content.Should().NotBeNullOrWhiteSpace();

				var doc = JToken.Parse(req.Content);
				doc["remove"].Should().NotBeNull();
				doc["remove"].Values<string>().Should().Contain("Banned");
			}
		}

		[Fact]
		public void ChatAuthRequest()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				var result = sut.GetChatAuthKeyAndEndpointsAsync().Result;

				// Assert
				result.Should().NotBeNull();
				result.Authkey.Should().Be(SimAuth.Value.ChatAuthKey);
				result.Endpoints.Should().BeEquivalentTo(Endpoints);
			}
		}

		[Fact]
		public void ParsesStreamStartAndRoundsCorrectly()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				var r = sut.GetStreamStartedAtAsync().Result;

				// Assert
				r.Should().NotBeNull();
				r.Value.Should().Be(StartedAtTestValue);
			}
		}

		[Fact]
		public void CanLookupUserId()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				var r = sut.LookupUserIdAsync(OtherUserName).Result;

				// Assert
				r.Should().Be(OtherUserId);
			}
		}

		[Fact]
		public void CanCallInitMultipleTimes()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);
				sut.InitAsync(ChannelName, null).Wait(Simulator.TIMEOUT);
			}
		}

		[Fact]
		public void LookupUserReturnNullIdUnknownUser()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				var userId = sut.LookupUserIdAsync("InvalidUserNameForSure").Result;
				userId.Should().BeNull();
			}
		}

		[Fact]
		public void CanGetChannelInfo()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				var (title, gameTypeId) = sut.GetChannelInfoAsync().Result;

				// Assert
				title.Should().Be("Test stream title 1");
				gameTypeId.Should().Be(GameTypeId);
			}
		}

		[Fact]
		public void CanUpdateGetChannelInfo()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				sut.UpdateChannelInfoAsync("New stream title", GameTypeId).Wait();

				var req = Handler.FindRequest($"channels/{ChannelId}", new HttpMethod("PATCH"));
				req.Should().NotBeNull();
				req.Content.Should().NotBeNullOrWhiteSpace();

				var doc = JToken.Parse(req.Content);
				doc["name"].Should().NotBeNull();
				doc["name"].Value<string>().Should().Be("New stream title");
				doc["typeId"].Should().NotBeNull();
				doc["typeId"].Value<uint>().Should().Be(GameTypeId);
			}
		}

		[Fact]
		public void CanLookupGameTypeByQuery()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				var gameTypes = sut.LookupGameTypeAsync("GameName").Result;

				// Assert
				gameTypes.Should().NotBeNull();
				gameTypes.Should().ContainSingle();
				gameTypes.First().Id.Should().Be(GameTypeId);
				gameTypes.First().Name.Should().Be("GameName");
			}
		}

		[Fact]
		public void CanLookupGameTypeById()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				var gameType = sut.LookupGameTypeByIdAsync(GameTypeId).Result;

				// Assert
				gameType.Should().NotBeNull();
				gameType.Id.Should().Be(GameTypeId);
				gameType.Name.Should().Be("TestGameName");
			}
		}

		[Fact]
		public void HandlesUnknownGameTypeById()
		{
			using (var sut = new MixerRestClient(LoggerFactory, Client))
			{
				sut.InitAsync(ChannelName, Token).Wait(Simulator.TIMEOUT);

				var gameType = sut.LookupGameTypeByIdAsync(34634).Result;

				// Assert
				gameType.Should().BeNull();
			}
		}
	}
}
