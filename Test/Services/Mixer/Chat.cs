using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Fritz.StreamTools.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Test.Services.Mixer
{
	public class Chat : Base
	{
		[Fact]
		public void WillConnectAndJoin()
		{
			var sim = SimAuth.Value;
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

				var connectedAndJoined = ws.JoinedChat.Wait(Simulator.TIMEOUT);
				connectedAndJoined.Should().BeTrue();

				ws.LastPacket["method"].Should().NotBeNull();
				ws.LastPacket["method"].Value<string>().Should().Be("auth");
				ws.LastPacket["arguments"].Should().NotBeNull();
				var expectedArgs = $"[{sim.ChannelInfo.Id},{sim.ChannelInfo.UserId},\"{sim.ChatAuthKey}\"]";
				var arguments = ws.LastPacket["arguments"].ToString(Formatting.None);
				arguments.Should().Be(expectedArgs);
			}
		}

		[Fact]
		public void WillConnectAndJoinAnonymously()
		{
			var sim = SimAnon.Value;

			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

				var connectedAndJoined = ws.JoinedChat.Wait(Simulator.TIMEOUT);
				connectedAndJoined.Should().BeTrue();

				ws.LastPacket["method"].Should().NotBeNull();
				ws.LastPacket["method"].Value<string>().Should().Be("auth");
				ws.LastPacket["arguments"].Should().NotBeNull();
				var args = ws.LastPacket["arguments"].ToString(Formatting.None);
				args.Should().Be($"[{sim.ChannelInfo.Id}]");
			}
		}

		[Fact]
		public void WillReconnectToNextServer()
		{
			var sim = SimAnon.Value;
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

				var url1 = ws.ConnectUrl;

				// Prepare new ClientWebSocket for consumption by client code, and dispose the old one
				sim.ChatWebSocket = new SimulatedClientWebSocket(true, false, Simulator.CHAT_WELCOME);
				ws.Dispose();
				ws = sim.ChatWebSocket;
				bool reconnectSucceeded = ws.JoinedChat.Wait(Simulator.TIMEOUT);

				var url2 = ws.ConnectUrl;

				reconnectSucceeded.Should().BeTrue();
				url1.Should().NotBeNull();
				url2.Should().NotBeNull();
				url1.Should().NotBe(url2);
			}
		}

		[Fact]
		public void RaisesEventOnMessage()
		{
			var sim = SimAnon.Value;
			var packet = BuildChatMessage(sim, 56789, "connor", "Hello world!", roles: new string[] { "Owner" });
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(packet);
					monitor.Should().Raise(nameof(sut.ChatMessage))
						.WithArgs<ChatMessageEventArgs>(a => a.Message == "Hello world!" && a.UserName == "connor" && a.UserId == 56789
																						&& !a.IsModerator && a.IsOwner && !a.IsWhisper && a.ChannelId == sim.ChannelInfo.Id)
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public void RaisesEventOnWhisper()
		{
			var sim = SimAnon.Value;
			var packet = BuildChatMessage(sim, 56789, "connor", "Hello world!", roles: new string[] { "Owner" }, isWhisper: true);
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(packet);
					monitor.Should().Raise(nameof(sut.ChatMessage))
						.WithArgs<ChatMessageEventArgs>(a => a.Message == "Hello world!" && a.UserName == "connor" && a.UserId == 56789
																						&& !a.IsModerator && a.IsOwner && a.IsWhisper && a.ChannelId == sim.ChannelInfo.Id)
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public void DetectsUserRoles()
		{
			var sim = SimAnon.Value;
			var packet = BuildChatMessage(sim, 56789, "connor", "Hello world!", roles: new string[] { "Owner", "Mod" });
			var ws = sim.ChatWebSocket;

			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(packet);
					monitor.Should().Raise(nameof(sut.ChatMessage))
						.WithArgs<ChatMessageEventArgs>(a => a.Message == "Hello world!" && a.UserName == "connor" && a.UserId == 56789 && a.IsModerator && a.IsOwner && !a.IsWhisper)
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public void RaisesUserJoinsEvent()
		{
			var sim = SimAnon.Value;
			var packet = BuildUserJoinOrLeave(sim, "SomeNewUser", 34103083, isJoin: true);
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(packet);
					monitor.Should().Raise(nameof(sut.UserJoined))
						.WithArgs<ChatUserInfoEventArgs>(a => a.UserId == 34103083 && a.UserName == "SomeNewUser" && a.ServiceName == "Mixer" && a.ChannelId == sim.ChannelInfo.Id)
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public void RaisesUserLeftEvent()
		{
			var sim = SimAnon.Value;
			var packet = BuildUserJoinOrLeave(sim, "SomeNewUser", 34103083, isJoin: false);
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(packet);
					monitor.Should().Raise(nameof(sut.UserLeft))
						.WithArgs<ChatUserInfoEventArgs>(a => a.UserId == 34103083 && a.UserName == "SomeNewUser" && a.ServiceName == "Mixer" && a.ChannelId == sim.ChannelInfo.Id)
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public void ImplementsCorrectInterfaces()
		{
			var sim = SimAnon.Value;
			var packet = BuildChatMessage(sim, 56789, "connor", "Hello world!");
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
				var result = Assert.Raises<ChatMessageEventArgs>(x => sut.ChatMessage += x, x => sut.ChatMessage -= x, () => ws.InjectPacket(packet));
				Assert.IsAssignableFrom<IChatService>(result.Sender);
				Assert.IsAssignableFrom<IStreamService>(result.Sender);
			}
		}

		[Fact]
		public void AddCorrectHeaders()
		{
			var sim = SimAuth.Value;
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

				ws.Headers.Should().Contain("x-is-bot", "true");
			}
		}

		[Fact]
		public void HandlesNullAvatar()
		{
			var sim = SimAnon.Value;
			var packet = BuildChatMessage(sim, 56789, "connor", "Hello world!", roles: new string[] { "Owner", "Mod" }, avatar: null);
			var ws = sim.ChatWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(packet);
					monitor.Should().Raise(nameof(sut.ChatMessage))
						.WithArgs<ChatMessageEventArgs>(a => a.Properties.ContainsKey("AvatarUrl") && a.UserName == "connor")
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public void CanSendMessage()
		{
			var sim = SimAuth.Value;
			var ws = sim.ChatWebSocket;

			const string text = "Some test message";

			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

				var chat = sut as IChatService;
				var id = ws.LastId.GetValueOrDefault() + 1;
				var replyJson = BuildMsgReply(sim, id, text);

				var task = chat.SendMessageAsync(text);
				ws.InjectPacket(replyJson);

				task.Wait(Simulator.TIMEOUT); // Wait for SendMessageAsync to complete
				task.Result.Should().BeTrue();
			}
		}

		[Fact]
		public void CanSendWhisper()
		{
			var sim = SimAuth.Value;
			var ws = sim.ChatWebSocket;

			const string text = "Some test message";
			const string target = "OtherUser";

			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

				var chat = sut as IChatService;
				var id = ws.LastId.GetValueOrDefault() + 1;
				var replyJson = BuildMsgReply(sim, id, text, target);

				var task = chat.SendWhisperAsync(target, text);
				ws.InjectPacket(replyJson);

				task.Wait(Simulator.TIMEOUT); // Wait for SendWhisperAsync to complete
				task.Result.Should().BeTrue();
			}
		}

		[Fact]
		public void CantSendAnononymously()
		{
			var sim = SimAnon.Value;
			var ws = sim.ChatWebSocket;

			const string text = "Some test message";

			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

				var chat = sut as IChatService;
				var id = ws.LastId.GetValueOrDefault() + 1;
				var replyJson = BuildMsgReply(sim, id, text);

				var task = chat.SendMessageAsync(text);
				ws.InjectPacket(replyJson);

				task.Wait(Simulator.TIMEOUT); // Wait for SendMessageAsync to complete
				task.Result.Should().BeFalse();
			}
		}

		[Fact]
		public void TimeoutUserSendsCorrectPackets()
		{
			var sim = SimAuth.Value;
			var ws = sim.ChatWebSocket;

			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

				var chat = sut as IChatService;
				var id = ws.LastId.GetValueOrDefault() + 1;
				var replyJson = BuildTimeoutReply(id);

				var task = chat.TimeoutUserAsync("SomeTestUser", TimeSpan.FromMinutes(5));
				ws.InjectPacket(replyJson);

				task.Wait(Simulator.TIMEOUT); // Wait for TimeoutUserAsync to complete

				// Validate packet sent
				ws.LastPacket["type"].Should().NotBeNull();
				ws.LastPacket["type"].Value<string>().Should().Be("method");
				ws.LastPacket["method"].Should().NotBeNull();
				ws.LastPacket["method"].Value<string>().Should().Be("timeout");
				ws.LastPacket["arguments"].Should().NotBeNull();
				var args = ws.LastPacket["arguments"].Values<string>().ToArray();
				args.Should().HaveCount(2);
				args[0].Should().Be("SomeTestUser");
				args[1].Should().Be("5m0s");

				task.Result.Should().BeTrue();
			}
		}

		[Fact]
		public void CanHandleRealDataDump()
		{
			var sim = SimAuth.Value;
			var ws = sim.ConstellationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

				foreach (var line in File.ReadAllLines("Services/Mixer/Data/ChatDump.json"))
				{
					if (string.IsNullOrWhiteSpace(line))
						continue;
					ws.InjectPacket(line);
				}

				sut.StopAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
			}
		}
	}
}
