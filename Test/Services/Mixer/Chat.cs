using System;
using System.Threading.Tasks;
using FluentAssertions;
using Fritz.StreamTools.Helpers;
using Fritz.StreamTools.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Test.Services.Mixer
{
	public class Chat : Base
	{
		[Fact]
		public async Task WillConnectAndJoin()
		{
			var sim = SimAuth.Value;
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);

				ws.JoinedChat.Should().BeTrue();
				ws.LastPacket["method"].Should().NotBeNull();
				ws.LastPacket["method"].Value<string>().Should().Be("auth");
				ws.LastPacket["arguments"].Should().NotBeNull();
				var expectedArgs = $"[{sim.ChannelInfo.Id},{sim.ChannelInfo.UserId},\"{sim.ChatAuthKey}\"]";
				var arguments = ws.LastPacket["arguments"].ToString(Formatting.None);
				arguments.Should().Be(expectedArgs);
			}
		}

		[Fact]
		public async Task WillConnectAndJoinAnonymously()
		{
			var sim = SimAnon.Value;

			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);

				ws.JoinedChat.Should().BeTrue();
				ws.LastPacket["method"].Should().NotBeNull();
				ws.LastPacket["method"].Value<string>().Should().Be("auth");
				ws.LastPacket["arguments"].Should().NotBeNull();
				var args = ws.LastPacket["arguments"].ToString(Formatting.None);
				args.Should().Be($"[{sim.ChannelInfo.Id}]");
			}
		}

		[Fact]
		public async Task RaisesEventOnMessage()
		{
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_roles':['Owner'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}]}}}".Replace("'", "\"");

			var sim = SimAnon.Value;
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sut.ChatMessage))
						.WithArgs<ChatMessageEventArgs>(a => a.Message == "Hello world!" && a.UserName == "connor" && a.UserId == 56789 && !a.IsModerator && a.IsOwner && !a.IsWhisper)
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public async Task RaisesEventOnWhisper()
		{
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_roles':['Owner'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}],'meta':{'whisper':true}}}}".Replace("'", "\"");

			var sim = SimAnon.Value;
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sut.ChatMessage))
						.WithArgs<ChatMessageEventArgs>(a => a.Message == "Hello world!" && a.UserName == "connor" && a.UserId == 56789 && !a.IsModerator && a.IsOwner && a.IsWhisper)
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public async Task DetectsUserRoles()
		{
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_roles':['Owner','Mod'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}]}}}".Replace("'", "\"");

			var sim = SimAnon.Value;
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sut.ChatMessage))
						.WithArgs<ChatMessageEventArgs>(a => a.Message == "Hello world!" && a.UserName == "connor" && a.UserId == 56789 && a.IsModerator && a.IsOwner && !a.IsWhisper)
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public async Task RaisesUserJoinsEvent()
		{
			var PACKET = "{'type':'event','event':'UserJoin','data':{'originatingChannel':1234,'username':'SomeNewUser','roles':['User'],'id':34103083}}".Replace("'", "\"");

			var sim = SimAnon.Value;
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sut.UserJoined))
						.WithArgs<ChatUserInfoEventArgs>(a => a.UserId == 34103083 && a.UserName == "SomeNewUser" && a.ServiceName == "Mixer")
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public async Task RaisesUserLeftEvent()
		{
			var PACKET = "{'type':'event','event':'UserLeave','data':{'originatingChannel':1234,'username':'TheWhisperUser','roles':['User'],'id':34103083}}".Replace("'", "\"");

			var sim = SimAnon.Value;
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sut.UserLeft))
						.WithArgs<ChatUserInfoEventArgs>(a => a.UserId == 34103083 && a.UserName == "TheWhisperUser" && a.ServiceName == "Mixer")
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public async Task ImplementsCorrectInterfaces()
		{
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_roles':['Owner'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}]}}}".Replace("'", "\"");

			var sim = SimAnon.Value;
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);
				var result = Assert.Raises<ChatMessageEventArgs>(x => sut.ChatMessage += x, x => sut.ChatMessage -= x, () => ws.InjectPacket(PACKET).Wait());
				Assert.IsAssignableFrom<IChatService>(result.Sender);
				Assert.IsAssignableFrom<IStreamService>(result.Sender);
			}
		}

		[Fact]
		public async Task HandlesNullAvatar()
		{
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_avatar':null,'user_roles':['Owner','Mod'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}]}}}".Replace("'", "\"");

			var sim = SimAnon.Value;
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sut.ChatMessage))
						.WithArgs<ChatMessageEventArgs>(a => a.Properties.ContainsKey("AvatarUrl") && a.UserName == "connor")
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public async Task CanSendMessage()
		{

			var sim = SimAuth.Value;
			var ws = sim.ChatWebSocket;

			const string text = "Some test message";

			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);

				var chat = sut as IChatService;
				var id = ws.LastId.GetValueOrDefault() + 1;
				var replyJson = BuildMsgReply(sim, id, text);

				var task = chat.SendMessageAsync(text);
				await ws.InjectPacket(replyJson);

				await task; // Wait for SendMessageAsync to complete
				task.Result.Should().BeTrue();
			}
		}

		[Fact]
		public async Task CanSendWhisper()
		{
			var sim = SimAuth.Value;
			var ws = sim.ChatWebSocket;

			const string text = "Some test message";
			const string target = "OtherUser";

			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);

				var chat = sut as IChatService;
				var id = ws.LastId.GetValueOrDefault() + 1;
				var replyJson = BuildWhisperReply(sim, id, target, text);

				var task = chat.SendWhisperAsync(target, text);
				await ws.InjectPacket(replyJson);

				await task;	// Wait for SendWhisperAsync to complete
				task.Result.Should().BeTrue();
			}
		}
	}
}
