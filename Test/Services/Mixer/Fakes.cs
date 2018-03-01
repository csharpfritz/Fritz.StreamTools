using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Services.Mixer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Test.Services.Mixer
{
	public class FakeJsonRpcWebSocket : IJsonRpcWebSocket
	{
		readonly JsonRpcWebSocket _realObject;

		public FakeJsonRpcWebSocket(ILogger logger, IMixerFactory factory, bool isChat)
		{
			_realObject = new JsonRpcWebSocket(logger, factory, isChat);
		}

		public bool IsAuthenticated { get; } = true;
		public event EventHandler<EventEventArgs> EventReceived
		{
			add => _realObject.EventReceived += value;
			remove => _realObject.EventReceived -= value;
		}

		public void Dispose() { }
		virtual public Task<bool> SendAsync(string method, params object[] args) => Task.FromResult(true);
		virtual public Task<bool> TryConnectAsync(Func<string> resolveUrl, string accessToken, Func<Task> postConnectFunc) => Task.FromResult(true);
		public void InjectTestPacket(string json) => _realObject.InjectTestPacket(json);
	}

	public class FakeMixerFactory : IMixerFactory
	{
		private readonly Mock<IMixerRestClient> _restClientMock;

		public ChannelInfo ChannelInfo { get; } = new ChannelInfo { Id = 1234, UserId = 56789, NumberOfFollowers = -1, NumberOfViewers = -1 };
		public string[] Endpoints { get; } = new string[] { "first.test.invalid", "second.test.invalid" };

		public IMixerLive LiveObject { get; set; }
		public IMixerChat ChatObject { get; set; }

		internal FakeJsonRpcWebSocket LiveChannel { get; set; }
		internal FakeJsonRpcWebSocket ChatChannel { get; set; }

		public FakeMixerFactory(IConfiguration config, ILoggerFactory loggerFactory)
		{
			LiveChannel = new FakeJsonRpcWebSocket(new Mock<ILogger>().Object, this, false);
			ChatChannel = new FakeJsonRpcWebSocket(new Mock<ILogger>().Object, this, true);

			_restClientMock = new Mock<IMixerRestClient>();
			_restClientMock.Setup(x => x.GetChannelIdAsync()).Returns(Task.FromResult(ChannelInfo.Id));
			_restClientMock.Setup(x => x.GetChannelInfoAsync()).Returns(Task.FromResult(ChannelInfo));
			_restClientMock.Setup(x => x.GetChatAuthKeyAndEndpointsAsync()).Returns(Task.FromResult(new ChatAuthKeyAndEndpoints { AuthKey = "zxc1234", Endpoints = Endpoints }));

			LiveObject = new MixerLive(config, loggerFactory, this, CancellationToken.None);
			ChatObject = new MixerChat(config, loggerFactory, this, _restClientMock.Object, CancellationToken.None);
		}

		public void InjectLivePacket(string json) => LiveChannel.InjectTestPacket(json);
		public void InjectChatPacket(string json) => ChatChannel.InjectTestPacket(json);

		public ClientWebSocket CreateClientWebSocket() => new Mock<ClientWebSocket>().Object;
		public IJsonRpcWebSocket CreateJsonRpcWebSocket(ILogger logger, bool isChat) => ( isChat ) ? ChatChannel : LiveChannel;
		public IMixerChat CreateMixerChat(IMixerRestClient client, CancellationToken shutdownRequest) => ChatObject;
		public IMixerLive CreateMixerLive(CancellationToken shutdownRequest) => LiveObject;
		public IMixerRestClient CreateMixerRestClient(string channelName, string token = null) => _restClientMock.Object;
	}
}
