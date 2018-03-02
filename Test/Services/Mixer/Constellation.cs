using System.Threading.Tasks;
using FluentAssertions;
using Fritz.StreamTools.Helpers;
using Fritz.StreamTools.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

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
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);

				ws.JoinedConstallation.Should().BeTrue();
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
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					await ws.InjectPacket(PACKET);
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
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					await ws.InjectPacket(PACKET);
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
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);
				await ws.InjectPacket(PACKET);		// 1st
				using (var monitor = sut.Monitor())
				{
					await ws.InjectPacket(PACKET);	// 2nd
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
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);
				using (var monitor = sut.Monitor())
				{
					await ws.InjectPacket(PACKET);
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
				await sut.StartAsync(sim.Cancel.Token).OrTimeout(sim.START_TIMEOUT);
				var result = Assert.Raises<ServiceUpdatedEventArgs>(x => sut.Updated += x, x => sut.Updated -= x, () => ws.InjectPacket(PACKET).Wait() );
				Assert.IsAssignableFrom<IChatService>(result.Sender);
				Assert.IsAssignableFrom<IStreamService>(result.Sender);
			}
		}
	}
}
