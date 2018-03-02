using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Test.Services.Mixer
{
	public class MixerServiceShould
  {
		private readonly LoggerFactory _loggerFactory;
		private readonly IConfiguration _config;
		private readonly FakeMixerFactory _fakeFactory;

		public MixerServiceShould()
		{
			_loggerFactory = new LoggerFactory();
			var set = new Dictionary<string, string>() {
				{ "StreamServices:Mixer:Channel", "MyChannel" },
				{ "StreamServices:Mixer:Token", "abcd1234" }
			};
			_config = new ConfigurationBuilder().AddInMemoryCollection(set).Build();
			_fakeFactory = new FakeMixerFactory(_config, _loggerFactory);
		}

		private MixerService InitAndStartMixerService()
		{
			var mixer = new MixerService(_config, _loggerFactory, _fakeFactory);
			mixer.StartAsync(CancellationToken.None).Wait();
			return mixer;
		}

		[Fact]
		public void TryToConnectToChatEndpoint()
		{
			// Arrange
			var mock = new Mock<FakeJsonRpcWebSocket>(_loggerFactory.CreateLogger("test"), _fakeFactory, true);
			mock.Setup(x => x.TryConnectAsync(It.IsAny<Func<string>>(), It.IsAny<string>(), It.IsAny<Func<Task>>())).Returns(Task.FromResult(true)).Verifiable();
			_fakeFactory.ChatChannel = mock.Object;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			mock.Verify();
		}

		[Fact]
		public void TryToConnectToLiveEndpoint()
		{
			// Arrange
			var mock = new Mock<FakeJsonRpcWebSocket>(_loggerFactory.CreateLogger("test"), _fakeFactory, false);
			mock.Setup(x => x.TryConnectAsync(It.IsAny<Func<string>>(), It.IsAny<string>(), It.IsAny<Func<Task>>())).Returns(Task.FromResult(true)).Verifiable();
			_fakeFactory.LiveChannel = mock.Object;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			mock.Verify();
		}

		[Fact]
		public void RaiseEventWhenFollowersChanges()
		{
			// Arrange
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'numFollowers':66}}}".Replace("'", "\"");
			var channel = _fakeFactory.LiveChannel;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			var result = Assert.Raises<ServiceUpdatedEventArgs>(x => sut.Updated += x, x => sut.Updated -= x, () => channel.InjectTestPacket(PACKET));
			Assert.Equal("Mixer", result.Arguments.ServiceName);
			Assert.Equal(66, result.Arguments.NewFollowers);
			Assert.Null(result.Arguments.NewViewers);
			Assert.Null(result.Arguments.IsOnline);
		}

		[Fact]
		public void RaiseEventWhenViewersChanges()
		{
			// Arrange
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'viewersCurrent':35}}}".Replace("'", "\"");
			var channel = _fakeFactory.LiveChannel;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			var result = Assert.Raises<ServiceUpdatedEventArgs>(x => sut.Updated += x, x => sut.Updated -= x, () => channel.InjectTestPacket(PACKET));
			Assert.Equal("Mixer", result.Arguments.ServiceName);
			Assert.Equal(35, result.Arguments.NewViewers);
			Assert.Null(result.Arguments.NewFollowers);
			Assert.Null(result.Arguments.IsOnline);
		}

		[Fact]
		public void NotRaiseEventWhenViewersIsSameAsBefore()
		{
			// Arrange
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'viewersCurrent':35}}}".Replace("'", "\"");
			var channel = _fakeFactory.LiveChannel;
			var eventRaised = false;

			// Act
			var sut = InitAndStartMixerService();

			channel.InjectTestPacket(PACKET);	// 1st
			sut.Updated += (s, e) => eventRaised = true;
			channel.InjectTestPacket(PACKET);	// 2nd

			// Assert
			Assert.False(eventRaised);
		}

		[Fact]
		public void CombineLiveEventToSingleRaisedEvent()
		{
			// Arrange
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'viewersCurrent':43,'numFollowers':22,'online':true}}}".Replace("'", "\"");
			var channel = _fakeFactory.LiveChannel;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			var result = Assert.Raises<ServiceUpdatedEventArgs>(x => sut.Updated += x, x => sut.Updated -= x, () => channel.InjectTestPacket(PACKET));
			Assert.Equal("Mixer", result.Arguments.ServiceName);
			Assert.Equal(43, result.Arguments.NewViewers);
			Assert.Equal(22, result.Arguments.NewFollowers);
			Assert.True(result.Arguments.IsOnline);
		}

		[Fact]
		public void RaiseEventOnNewChatMessages()
		{
			// Arrange
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_roles':['Owner'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}]}}}".Replace("'", "\"");
			var channel = _fakeFactory.ChatChannel;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			var result = Assert.Raises<ChatMessageEventArgs>(x => sut.ChatMessage += x, x => sut.ChatMessage -= x, () => channel.InjectTestPacket(PACKET));
			Assert.Equal("Mixer", result.Arguments.ServiceName);
			Assert.Equal("Hello world!", result.Arguments.Message);
			Assert.Equal("connor", result.Arguments.UserName);
			Assert.Equal(56789, result.Arguments.UserId);
			Assert.False(result.Arguments.IsModerator);
			Assert.True(result.Arguments.IsOwner);
			Assert.False(result.Arguments.IsWhisper);
		}

		[Fact]
		public void RaiseEventOnWhisperMessages()
		{
			// Arrange
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_roles':['Owner'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}],'meta':{'whisper':true}}}}".Replace("'", "\"");
			var channel = _fakeFactory.ChatChannel;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			var result = Assert.Raises<ChatMessageEventArgs>(x => sut.ChatMessage += x, x => sut.ChatMessage -= x, () => channel.InjectTestPacket(PACKET));
			Assert.Equal("Mixer", result.Arguments.ServiceName);
			Assert.Equal("Hello world!", result.Arguments.Message);
			Assert.Equal("connor", result.Arguments.UserName);
			Assert.Equal(56789, result.Arguments.UserId);
			Assert.False(result.Arguments.IsModerator);
			Assert.True(result.Arguments.IsOwner);
			Assert.True(result.Arguments.IsWhisper);
		}

		[Fact]
		public void DetectsUserRolesFromChatMessages()
		{
			// Arrange
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_roles':['Owner','Mod'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}]}}}".Replace("'", "\"");
			var channel = _fakeFactory.ChatChannel;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			var result = Assert.Raises<ChatMessageEventArgs>(x => sut.ChatMessage += x, x => sut.ChatMessage -= x, () => channel.InjectTestPacket(PACKET));
			Assert.Equal("Mixer", result.Arguments.ServiceName);
			Assert.Equal("Hello world!", result.Arguments.Message);
			Assert.Equal("connor", result.Arguments.UserName);
			Assert.Equal(56789, result.Arguments.UserId);
			Assert.True(result.Arguments.IsModerator);
			Assert.True(result.Arguments.IsOwner);
			Assert.False(result.Arguments.IsWhisper);
		}

		[Fact]
		public void RaiseEventWhenUserJoinsChat()
		{
			// Arrange
			var PACKET = "{'type':'event','event':'UserJoin','data':{'originatingChannel':1234,'username':'SomeNewUser','roles':['User'],'id':34103083}}".Replace("'", "\"");
			var channel = _fakeFactory.ChatChannel;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			var result = Assert.Raises<ChatUserInfoEventArgs>(x => sut.UserJoined += x, x => sut.UserJoined -= x, () => channel.InjectTestPacket(PACKET));
			Assert.Equal("Mixer", result.Arguments.ServiceName);
			Assert.Equal("SomeNewUser", result.Arguments.UserName);
			Assert.Equal(34103083, result.Arguments.UserId);
		}

		[Fact]
		public void RaiseEventWhenUserLeavesChat()
		{
			// Arrange
			var PACKET = "{'type':'event','event':'UserLeave','data':{'originatingChannel':1234,'username':'TheWhisperUser','roles':['User'],'id':34103083}}".Replace("'", "\"");
			var channel = _fakeFactory.ChatChannel;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			var result = Assert.Raises<ChatUserInfoEventArgs>(x => sut.UserLeft += x, x => sut.UserLeft -= x, () => channel.InjectTestPacket(PACKET));
			Assert.Equal("Mixer", result.Arguments.ServiceName);
			Assert.Equal("TheWhisperUser", result.Arguments.UserName);
			Assert.Equal(34103083, result.Arguments.UserId);
		}

		[Fact]
		public void ImplementCorrectInterfacesOnChatEvents()
		{
			// Arrange
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_roles':['Owner'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}]}}}".Replace("'", "\"");
			var channel = _fakeFactory.ChatChannel;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			var result = Assert.Raises<ChatMessageEventArgs>(x => sut.ChatMessage += x, x => sut.ChatMessage -= x, () => channel.InjectTestPacket(PACKET));
			Assert.IsAssignableFrom<IChatService>(result.Sender);
			Assert.IsAssignableFrom<IStreamService>(result.Sender);
		}

		[Fact]
		public void ImplementCorrectInterfacesOnLiveEvents()
		{
			// Arrange
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'numFollowers':66}}}".Replace("'", "\"");
			var channel = _fakeFactory.LiveChannel;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			var result = Assert.Raises<ServiceUpdatedEventArgs>(x => sut.Updated += x, x => sut.Updated -= x, () => channel.InjectTestPacket(PACKET));
			Assert.IsAssignableFrom<IChatService>(result.Sender);
			Assert.IsAssignableFrom<IStreamService>(result.Sender);
		}

		[Fact]
		public void HandleNullAvatarInChatMessage()
		{
			// Arrange
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_avatar':null,'user_roles':['Owner','Mod'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}]}}}".Replace("'", "\"");
			var channel = _fakeFactory.ChatChannel;

			// Act
			var sut = InitAndStartMixerService();

			// Assert
			var result = Assert.Raises<ChatMessageEventArgs>(x => sut.ChatMessage += x, x => sut.ChatMessage -= x, () => channel.InjectTestPacket(PACKET));
			Assert.Equal("Mixer", result.Arguments.ServiceName);
			Assert.Equal("Hello world!", result.Arguments.Message);
			Assert.Equal("connor", result.Arguments.UserName);
			Assert.Equal(56789, result.Arguments.UserId);
			Assert.True(result.Arguments.IsModerator);
			Assert.True(result.Arguments.IsOwner);
			Assert.False(result.Arguments.IsWhisper);
		}
	}
}
