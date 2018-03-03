using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Services.Mixer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Test.Services.Mixer
{
	public class Simulator : IMixerFactory
	{
		public const int TIMEOUT = 1000;
		public static readonly string CHAT_WELCOME = "{'type':'event','event':'WelcomeEvent','data':{'server':'fac96c06-8314-41dd-9092-7e717ec2ee52'}}".Replace("'", "\"");
		public static readonly string CONSTALLATION_WELCOME = "{'type':'event','event':'hello','data':{'authenticated':false}}".Replace("'", "\"");

		private readonly Mock<IMixerRestClient> _restClientMock;
		private readonly ILoggerFactory _loggerFactory;
		public IConfiguration Config { get; }

		public ChannelInfo ChannelInfo { get; } = new ChannelInfo { Id = 1234, UserId = 56789, NumberOfFollowers = -1, NumberOfViewers = -1 };
		public string[] Endpoints { get; } = new string[] { "wss://first.test.com", "wss://second.test.com" };
		public bool HasToken { get; }
		public string ChatAuthKey { get; }
		public string UserName { get; }
		public ITestOutputHelper Output { get; set; }

		public CancellationTokenSource Cancel { get; } = new CancellationTokenSource();
		public SimulatedClientWebSocket ChatWebSocket { get; set; }
		public SimulatedClientWebSocket ConstallationWebSocket { get; set; }

		public Simulator(IConfiguration config, ILoggerFactory loggerFactory, ITestOutputHelper output)
		{
			Output = output ?? throw new ArgumentNullException(nameof(output));
			Config = config ?? throw new System.ArgumentNullException(nameof(config));
			_loggerFactory = loggerFactory ?? throw new System.ArgumentNullException(nameof(loggerFactory));

			var channelName = Config["StreamServices:Mixer:Channel"];
			HasToken = !string.IsNullOrEmpty(Config["StreamServices:Mixer:Token"]);
			ChatAuthKey = HasToken ? "zxc1234" : null;
			UserName = HasToken ? "TestUser" : null;

		ChatWebSocket = new SimulatedClientWebSocket(true, HasToken, CHAT_WELCOME) { Output = Output };
		ConstallationWebSocket = new SimulatedClientWebSocket(false, HasToken, CONSTALLATION_WELCOME) { Output = Output };

		_restClientMock = new Mock<IMixerRestClient>();
			_restClientMock.Setup(x => x.GetChannelIdAsync()).Returns(Task.FromResult(ChannelInfo.Id));
			_restClientMock.Setup(x => x.GetChannelInfoAsync()).Returns(Task.FromResult(ChannelInfo));
			_restClientMock.Setup(x => x.GetChatAuthKeyAndEndpointsAsync())
				.Returns(Task.FromResult(new ChatAuthKeyAndEndpoints { AuthKey = ChatAuthKey, Endpoints = Endpoints }));
			_restClientMock.Setup(x => x.ChannelName).Returns(channelName);
			_restClientMock.Setup(x => x.UserName).Returns(UserName);
			_restClientMock.Setup(x => x.UserId).Returns(ChannelInfo.UserId);
			_restClientMock.Setup(x => x.HasToken).Returns(HasToken);
		}

		public IClientWebSocketProxy CreateClientWebSocket(bool isChat) => (isChat) ? ChatWebSocket : ConstallationWebSocket;
		public IJsonRpcWebSocket CreateJsonRpcWebSocket(ILogger logger, bool isChat) =>
			new JsonRpcWebSocket(new Mock<ILogger>().Object, this, Config, isChat) { SendTimeout = TimeSpan.FromMilliseconds(500) };
		public IMixerChat CreateChat(IMixerRestClient client, CancellationToken shutdownRequest) =>
			new MixerChat(Config, _loggerFactory, this, _restClientMock.Object, Cancel.Token);
		public IMixerConstallation CreateConstallation(CancellationToken shutdownRequest) => new MixerConstallation(Config, _loggerFactory, this, Cancel.Token);
		public IMixerRestClient CreateRestClient(string channelName, string token = null) => _restClientMock.Object;
	}
}
