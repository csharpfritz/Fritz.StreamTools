using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Fritz.StreamTools.Helpers;
using Fritz.StreamTools.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Test.Services.Mixer
{
	public class Chat
	{
		private readonly LoggerFactory _loggerFactory;
		private readonly IConfiguration _config;
		public Simulator Sim { get;  }

		public Chat()
		{
			_loggerFactory = new LoggerFactory();
			var set = new Dictionary<string, string>() {
				{ "StreamServices:Mixer:Channel", "MyChannel" },
				{ "StreamServices:Mixer:Token", "abcd1234" }
			};
			_config = new ConfigurationBuilder().AddInMemoryCollection(set).Build();
			Sim = new Simulator(_config, _loggerFactory);
		}

		[Fact]
		public async Task WillConnectAndJoin()
		{
			var ws = Sim.ChatWebSocket;
			using (var sim = new MixerService(_config, _loggerFactory, Sim))
			{
				await sim.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);

				ws.JoinedChat.Should().BeTrue();
				ws.LastPacket["method"].Should().NotBeNull();
				ws.LastPacket["method"].Value<string>().Should().Equals("auth");
				var args = $"[{Sim.ChannelInfo.Id},{Sim.ChannelInfo.UserId},\"{Sim.ChatAuthKey}\"]";
				ws.LastPacket["arguments"].Should().NotBeNull();
				ws.LastPacket["arguments"].ToString(Formatting.None).Should().Equals(args);
			}
		}

		[Fact]
		public async Task RaisesEventOnMessage()
		{
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_roles':['Owner'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}]}}}".Replace("'", "\"");

			var ws = Sim.ChatWebSocket;
			using (var sim = new MixerService(_config, _loggerFactory, Sim))
			{
				await sim.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);
				using (var monitor = sim.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sim.ChatMessage))
						.WithArgs<ChatMessageEventArgs>(a => a.Message == "Hello world!", a => a.UserName == "connor", a => a.UserId == 56789, a => !a.IsModerator, a => a.IsOwner, a => !a.IsWhisper)
						.WithSender(sim);
				}
			}
		}

		[Fact]
		public async Task RaisesEventOnWhisper()
		{
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_roles':['Owner'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}],'meta':{'whisper':true}}}}".Replace("'", "\"");

			var ws = Sim.ChatWebSocket;
			using (var sim = new MixerService(_config, _loggerFactory, Sim))
			{
				await sim.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);
				using (var monitor = sim.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sim.ChatMessage))
						.WithArgs<ChatMessageEventArgs>(a => a.Message == "Hello world!", a => a.UserName == "connor", a => a.UserId == 56789, a => !a.IsModerator, a => a.IsOwner, a => a.IsWhisper)
						.WithSender(sim);
				}
			}
		}

		[Fact]
		public async Task DetectsUserRoles()
		{
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_roles':['Owner','Mod'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}]}}}".Replace("'", "\"");

			var ws = Sim.ChatWebSocket;
			using (var sim = new MixerService(_config, _loggerFactory, Sim))
			{
				await sim.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);
				using (var monitor = sim.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sim.ChatMessage))
						.WithArgs<ChatMessageEventArgs>(a => a.Message == "Hello world!", a => a.UserName == "connor", a => a.UserId == 56789, a => a.IsModerator, a => a.IsOwner, a => !a.IsWhisper)
						.WithSender(sim);
				}
			}
		}

		[Fact]
		public async Task RaisesUserJoinsEvent()
		{
			var PACKET = "{'type':'event','event':'UserJoin','data':{'originatingChannel':1234,'username':'SomeNewUser','roles':['User'],'id':34103083}}".Replace("'", "\"");

			var ws = Sim.ChatWebSocket;
			using (var sim = new MixerService(_config, _loggerFactory, Sim))
			{
				await sim.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);
				using (var monitor = sim.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sim.UserJoined))
						.WithArgs<ChatUserInfoEventArgs>(a => a.UserId == 34103083, a => a.UserName == "SomeNewUser", a => a.ServiceName == "Mixer")
						.WithSender(sim);
				}
			}
		}

		[Fact]
		public async Task RaisesUserLeftEvent()
		{
			var PACKET = "{'type':'event','event':'UserLeave','data':{'originatingChannel':1234,'username':'TheWhisperUser','roles':['User'],'id':34103083}}".Replace("'", "\"");
			var ws = Sim.ChatWebSocket;
			using (var sim = new MixerService(_config, _loggerFactory, Sim))
			{
				await sim.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);
				using (var monitor = sim.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sim.UserLeft))
						.WithArgs<ChatUserInfoEventArgs>(a => a.UserId == 34103083, a => a.UserName == "TheWhisperUser", a => a.ServiceName == "Mixer")
						.WithSender(sim);
				}
			}
		}

		[Fact]
		public async Task ImplementsCorrectInterfaces()
		{
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_roles':['Owner'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}]}}}".Replace("'", "\"");

			var ws = Sim.ChatWebSocket;
			using (var sut = new MixerService(_config, _loggerFactory, Sim))
			{
				await sut.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);
				var result = Assert.Raises<ChatMessageEventArgs>(x => sut.ChatMessage += x, x => sut.ChatMessage -= x, () => ws.InjectPacket(PACKET).Wait());
				Assert.IsAssignableFrom<IChatService>(result.Sender);
				Assert.IsAssignableFrom<IStreamService>(result.Sender);
			}
		}

		[Fact]
		public async Task HandlesNullAvatar()
		{
			var PACKET = "{'type':'event','event':'ChatMessage','data':{'channel':1234,'id':'6351f9e0-3bf2-11e6-a3b3-bdc62094c158','user_name':'connor','user_id':56789,'user_avatar':null,'user_roles':['Owner','Mod'],'message':{'message':[{'type':'text','data':'Hello world ','text':'Hello world!'}]}}}".Replace("'", "\"");
			var ws = Sim.ChatWebSocket;
			using (var sut = new MixerService(_config, _loggerFactory, Sim))
			{
				await sut.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sut.ChatMessage))
						.WithArgs<ChatMessageEventArgs>(a => a.UserId == 56789, a => a.UserName == "connot", a => a.ServiceName == "Mixer", a => a.IsModerator, a => a.IsOwner, a => !a.IsWhisper)
						.WithSender(sut);
				}
			}
		}
	}
}
