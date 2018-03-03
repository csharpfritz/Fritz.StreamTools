using System;
using System.Threading.Tasks;
using FluentAssertions;
using Fritz.StreamTools.Helpers;
using Fritz.StreamTools.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Test.Services.Mixer
{
	public class Constellation : Base
	{
		[Fact]
		public async Task WillConnectAndJoin()
		{
			var sim = SimAnon.Value;
			var ws = sim.ConstallationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(Simulator.TIMEOUT);

				var connectedAndJoined = ws.JoinedConstallation.Wait(Simulator.TIMEOUT);

				connectedAndJoined.Should().BeTrue();
				// {{"id": 1,"type": "method","method": "livesubscribe","params": {"events": ["channel:1234:update" ]}}}
				ws.LastPacket["method"].Should().NotBeNull();
				ws.LastPacket["method"].Value<string>().Should().Be("livesubscribe");
				var args = $"[{sim.ChannelInfo.Id},{sim.ChannelInfo.UserId},\"{sim.ChatAuthKey}\"]";
				ws.LastPacket["params"].Should().NotBeNull();
				ws.LastPacket["params"]["events"].Should().NotBeNull();
				var events = ws.LastPacket["params"]["events"].ToString(Formatting.None);
				events.Should().ContainAll($"channel:{sim.ChannelInfo.Id}:update");
			}
		}

		[Fact]
		public async Task RaisesFollowerEvent()
		{
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'numFollowers':66}}}".Replace("'", "\"");

			var sim = SimAnon.Value;
			var ws = sim.ConstallationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(Simulator.TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sut.Updated))
						.WithArgs<ServiceUpdatedEventArgs>(a => a.NewFollowers == 66 && a.NewViewers == null && a.IsOnline == null && a.ServiceName == "Mixer")
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public async Task RaiseViewersEvent()
		{
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'viewersCurrent':35}}}".Replace("'", "\"");

			var sim = SimAnon.Value;
			var ws = sim.ConstallationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(Simulator.TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sut.Updated))
						.WithArgs<ServiceUpdatedEventArgs>(a => a.NewFollowers == null && a.NewViewers == 35 && a.IsOnline == null && a.ServiceName == "Mixer")
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public async Task DontRaiseEventWhenViewersIsSameAsBefore()
		{
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'viewersCurrent':35}}}".Replace("'", "\"");

			var sim = SimAnon.Value;
			var ws = sim.ConstallationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(Simulator.TIMEOUT);
				ws.InjectPacket(PACKET);		// 1st
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(PACKET);	// 2nd
					monitor.Should().NotRaise(nameof(sut.Updated));
				}
			}
		}

		[Fact]
		public async Task CanCombineEvent()
		{
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'viewersCurrent':43,'numFollowers':22,'online':true}}}".Replace("'", "\"");

			var sim = SimAnon.Value;
			var ws = sim.ConstallationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(Simulator.TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sut.Updated))
						.WithArgs<ServiceUpdatedEventArgs>(a => a.NewFollowers == 22 && a.NewViewers == 43 && a.IsOnline == true && a.ServiceName == "Mixer")
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public async Task ImplementCorrectInterfaces()
		{
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'numFollowers':66}}}".Replace("'", "\"");

			var sim = SimAnon.Value;
			var ws = sim.ConstallationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(Simulator.TIMEOUT);
				var result = Assert.Raises<ServiceUpdatedEventArgs>(x => sut.Updated += x, x => sut.Updated -= x, () => ws.InjectPacket(PACKET) );
				Assert.IsAssignableFrom<IChatService>(result.Sender);
				Assert.IsAssignableFrom<IStreamService>(result.Sender);
			}
		}

		[Fact]
		public void WillReconnect()
		{
			var sim = SimAnon.Value;
			var ws = sim.ConstallationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

				// Prepare new ClientWebSocket for consumption by client code, and dispose the old one
				sim.ConstallationWebSocket = new SimulatedClientWebSocket(false, false, Simulator.CONSTALLATION_WELCOME);
				ws.Dispose();
				ws = sim.ConstallationWebSocket;
				var connectedAndJoined = ws.JoinedConstallation.Wait(Simulator.TIMEOUT);

				connectedAndJoined.Should().BeTrue();
			}
		}

		[Fact]
		public async Task AddCorrectHeaders()
		{
			var sim = SimAuth.Value;
			var ws = sim.ConstallationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(Simulator.TIMEOUT);

				ws.Headers.Should().Contain("Authorization", $"Bearer {Token}");
				ws.Headers.Should().Contain("x-is-bot", "true");
			}
		}
	}
}
