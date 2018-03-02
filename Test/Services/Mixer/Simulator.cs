using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Services.Mixer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Test.Services.Mixer
{
	public class Simulator : IMixerFactory
	{
		public int START_TIMEOUT = 5000;
		static readonly string CHAT_WELCOME = "{'type':'event','event':'WelcomeEvent','data':{'server':'fac96c06-8314-41dd-9092-7e717ec2ee52'}}".Replace("'", "\"");
		static readonly string LIVE_WELCOME = "{'type':'event','event':'hello','data':{'authenticated':false}}".Replace("'", "\"");

		private readonly Mock<IMixerRestClient> _restClientMock;
		private readonly IConfiguration _config;
		private readonly ILoggerFactory _loggerFactory;

		public ChannelInfo ChannelInfo { get; } = new ChannelInfo { Id = 1234, UserId = 56789, NumberOfFollowers = -1, NumberOfViewers = -1 };
		public string[] Endpoints { get; } = new string[] { "wss://first.test.com", "wss://second.test.com" };
		public string ChatAuthKey { get; } = "zxc1234";

		public CancellationTokenSource Cancel { get; } = new CancellationTokenSource();
		public SimulatedClientWebSocket ChatWebSocket { get; set; } = new SimulatedClientWebSocket(true, CHAT_WELCOME);
		public SimulatedClientWebSocket ConstallationWebSocket { get; set; } = new SimulatedClientWebSocket(false, LIVE_WELCOME);

		public Simulator(IConfiguration config, ILoggerFactory loggerFactory)
		{
			_restClientMock = new Mock<IMixerRestClient>();
			_restClientMock.Setup(x => x.GetChannelIdAsync()).Returns(Task.FromResult(ChannelInfo.Id));
			_restClientMock.Setup(x => x.GetChannelInfoAsync()).Returns(Task.FromResult(ChannelInfo));
			_restClientMock.Setup(x => x.GetChatAuthKeyAndEndpointsAsync()).Returns(Task.FromResult(new ChatAuthKeyAndEndpoints { AuthKey = ChatAuthKey, Endpoints = Endpoints }));
			_config = config;
			_loggerFactory = loggerFactory;
		}

		public IClientWebSocketProxy CreateClientWebSocket(bool isChat) => (isChat) ? ChatWebSocket : ConstallationWebSocket;
		public IJsonRpcWebSocket CreateJsonRpcWebSocket(ILogger logger, bool isChat) => new JsonRpcWebSocket(new Mock<ILogger>().Object, this, isChat);
		public IMixerChat CreateChat(IMixerRestClient client, CancellationToken shutdownRequest) => new MixerChat(_config, _loggerFactory, this, _restClientMock.Object, Cancel.Token);
		public IMixerConstallation CreateConstallation(CancellationToken shutdownRequest) => new MixerConstallation(_config, _loggerFactory, this, Cancel.Token);
		public IMixerRestClient CreateRestClient(string channelName, string token = null) => _restClientMock.Object;
	}
}
