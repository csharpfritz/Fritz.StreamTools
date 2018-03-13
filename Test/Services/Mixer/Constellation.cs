using System.IO;
using FluentAssertions;
using Fritz.StreamTools.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Test.Services.Mixer
{
	public class Constellation : Base
	{
		[Fact]
		public void WillConnectAndJoin()
		{
			var sim = SimAnon.Value;
			var ws = sim.ConstellationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

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
		public void RaisesFollowerEvent()
		{
			var packet = BuildLiveEvent("channel:1234:update", followers: 66);

			var sim = SimAnon.Value;
			var ws = sim.ConstellationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(packet);
					monitor.Should().Raise(nameof(sut.Updated))
						.WithArgs<ServiceUpdatedEventArgs>(a => a.NewFollowers == 66 && a.NewViewers == null && a.IsOnline == null && a.ServiceName == "Mixer"
																							&& a.ChannelId == sim.ChannelInfo.Id)
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public void RaiseViewersEvent()
		{
			var packet = BuildLiveEvent("channel:1234:update", viewers: 735);

			var sim = SimAnon.Value;
			var ws = sim.ConstellationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(packet);
					monitor.Should().Raise(nameof(sut.Updated))
						.WithArgs<ServiceUpdatedEventArgs>(a => a.NewFollowers == null && a.NewViewers == 735 && a.IsOnline == null && a.ServiceName == "Mixer"
																							 && a.ChannelId == sim.ChannelInfo.Id)
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public void DontRaiseEventWhenViewersIsSameAsBefore()
		{
			var packet = BuildLiveEvent("channel:1234:update", viewers: 35);

			var sim = SimAnon.Value;
			var ws = sim.ConstellationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
				ws.InjectPacket(packet);    // 1st
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(packet);  // 2nd
					monitor.Should().NotRaise(nameof(sut.Updated));
				}
			}
		}

		[Fact]
		public void CanCombineEvent()
		{
			var packet = BuildLiveEvent("channel:1234:update", followers: 22, viewers: 43, online: true);

			var sim = SimAnon.Value;
			var ws = sim.ConstellationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					ws.InjectPacket(packet);
					monitor.Should().Raise(nameof(sut.Updated))
						.WithArgs<ServiceUpdatedEventArgs>(a => a.NewFollowers == 22 && a.NewViewers == 43 && a.IsOnline == true && a.ServiceName == "Mixer" && a.ChannelId == sim.ChannelInfo.Id)
						.WithSender(sut);
				}
			}
		}

		[Fact]
		public void ImplementCorrectInterfaces()
		{
			var packet = BuildLiveEvent("channel:1234:update", followers: 66);

			var sim = SimAnon.Value;
			var ws = sim.ConstellationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);
				var result = Assert.Raises<ServiceUpdatedEventArgs>(x => sut.Updated += x, x => sut.Updated -= x, () => ws.InjectPacket(packet));
				Assert.IsAssignableFrom<IChatService>(result.Sender);
				Assert.IsAssignableFrom<IStreamService>(result.Sender);
			}
		}

		[Fact]
		public void WillReconnect()
		{
			var sim = SimAnon.Value;
			var ws = sim.ConstellationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

				// Prepare new ClientWebSocket for consumption by client code, and dispose the old one
				sim.ConstellationWebSocket = new SimulatedClientWebSocket(false, false, Simulator.CONSTALLATION_WELCOME);
				ws.Dispose();
				ws = sim.ConstellationWebSocket;
				var connectedAndJoined = ws.JoinedConstallation.Wait(Simulator.TIMEOUT);

				connectedAndJoined.Should().BeTrue();
			}
		}

		[Fact]
		public void AddCorrectHeaders()
		{
			var sim = SimAuth.Value;
			var ws = sim.ConstellationWebSocket;
			using (var sut = new MixerService(sim.Config, LoggerFactory, sim))
			{
				sut.StartAsync(sim.Cancel.Token).Wait(Simulator.TIMEOUT);

				ws.Headers.Should().Contain("Authorization", $"Bearer {Token}");
				ws.Headers.Should().Contain("x-is-bot", "true");
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

				foreach (var line in File.ReadAllLines("Services/Mixer/Data/ConstellationDump.json"))
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
