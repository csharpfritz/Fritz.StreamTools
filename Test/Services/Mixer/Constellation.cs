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
	public class Constellation
	{
		private readonly LoggerFactory _loggerFactory;
		private readonly IConfiguration _config;
		public Simulator Sim { get;  }

		public Constellation()
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
			var ws = Sim.ConstallationWebSocket;
			using (var sim = new MixerService(_config, _loggerFactory, Sim))
			{
				await sim.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);

				ws.JoinedConstallation.Should().BeTrue();
				// {{"id": 1,"type": "method","method": "livesubscribe","params": {"events": ["channel:1234:update" ]}}}
				ws.LastPacket["method"].Should().NotBeNull();
				ws.LastPacket["method"].Value<string>().Should().Equals("livesubscribe");
				var args = $"[{Sim.ChannelInfo.Id},{Sim.ChannelInfo.UserId},\"{Sim.ChatAuthKey}\"]";
				ws.LastPacket["params"].Should().NotBeNull();
				ws.LastPacket["params"]["events"].Should().NotBeNull();
				ws.LastPacket["params"]["events"].ToString(Formatting.None).Should().ContainAll($"channel:{Sim.ChannelInfo.Id}:update");
			}
		}

		[Fact]
		public async Task RaisesFollowerEvent()
		{
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'numFollowers':66}}}".Replace("'", "\"");

			var ws = Sim.ConstallationWebSocket;
			using (var sim = new MixerService(_config, _loggerFactory, Sim))
			{
				await sim.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);
				using (var monitor = sim.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sim.Updated))
						.WithArgs<ServiceUpdatedEventArgs>(a => a.NewFollowers == 66, a => a.NewViewers == null, a => a.IsOnline == null, a => a.ServiceName == "Mixer")
						.WithSender(sim);
				}
			}
		}

		[Fact]
		public async Task RaiseViewersEvent()
		{
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'viewersCurrent':35}}}".Replace("'", "\"");
			var ws = Sim.ConstallationWebSocket;
			using (var sim = new MixerService(_config, _loggerFactory, Sim))
			{
				await sim.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);
				using (var monitor = sim.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sim.Updated))
						.WithArgs<ServiceUpdatedEventArgs>(a => a.NewFollowers == null, a => a.NewViewers == 35, a => a.IsOnline == null, a => a.ServiceName == "Mixer")
						.WithSender(sim);
				}
			}
		}

		[Fact]
		public async Task DontRaiseEventWhenViewersIsSameAsBefore()
		{
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'viewersCurrent':35}}}".Replace("'", "\"");

			var ws = Sim.ConstallationWebSocket;
			using (var sim = new MixerService(_config, _loggerFactory, Sim))
			{
				await sim.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);
				await ws.InjectPacket(PACKET);		// 1st
				using (var monitor = sim.Monitor())
				{
					await ws.InjectPacket(PACKET);	// 2nd
					monitor.Should().NotRaise(nameof(sim.Updated));
				}
			}
		}

		[Fact]
		public async Task CanCombineEvent()
		{
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'viewersCurrent':43,'numFollowers':22,'online':true}}}".Replace("'", "\"");

			var ws = Sim.ConstallationWebSocket;
			using (var sim = new MixerService(_config, _loggerFactory, Sim))
			{
				await sim.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);
				using (var monitor = sim.Monitor())
				{
					await ws.InjectPacket(PACKET);
					monitor.Should().Raise(nameof(sim.Updated))
						.WithArgs<ServiceUpdatedEventArgs>(a => a.NewFollowers == 22, a => a.NewViewers == 43, a => a.IsOnline == true, a => a.ServiceName == "Mixer")
						.WithSender(sim);
				}
			}
		}

		[Fact]
		public async Task ImplementCorrectInterfaces()
		{
			var PACKET = "{'type':'event','event':'live','data':{'channel':'channel:1234:update','payload':{'numFollowers':66}}}".Replace("'", "\"");

			var ws = Sim.ConstallationWebSocket;
			using (var sut = new MixerService(_config, _loggerFactory, Sim))
			{
				await sut.StartAsync(Sim.Cancel.Token).OrTimeout(Sim.START_TIMEOUT);
				var result = Assert.Raises<ServiceUpdatedEventArgs>(x => sut.Updated += x, x => sut.Updated -= x, () => ws.InjectPacket(PACKET).Wait() );
				Assert.IsAssignableFrom<IChatService>(result.Sender);
				Assert.IsAssignableFrom<IStreamService>(result.Sender);
			}
		}
	}
}
