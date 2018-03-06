using System;
using System.Diagnostics;
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
		public static int TIMEOUT { get => ( Debugger.IsAttached ) ? Timeout.Infinite : 2000; }
		public static readonly string CHAT_WELCOME = "{'type':'event','event':'WelcomeEvent','data':{'server':'fac96c06-8314-41dd-9092-7e717ec2ee52'}}".Replace("'", "\"");
		public static readonly string CONSTALLATION_WELCOME = "{'type':'event','event':'hello','data':{'authenticated':false}}".Replace("'", "\"");

		private readonly Mock<IMixerRestClient> _restClientMock;
		private readonly ILoggerFactory _loggerFactory;
		public IConfiguration Config { get; }

		public API.Channel ChannelInfo { get; } = new API.Channel { Id = 1234, UserId = 56789, NumFollowers = 0, ViewersCurrent = 0 };
		public string[] Endpoints { get; } = new string[] { "wss://first.test.com", "wss://second.test.com" };
		public bool HasToken { get; }
		public string ChatAuthKey { get; }
		public string UserName { get; }

		public CancellationTokenSource Cancel { get; } = new CancellationTokenSource();
		public SimulatedClientWebSocket ChatWebSocket { get; set; }
		public SimulatedClientWebSocket ConstellationWebSocket { get; set; }

		public Simulator(IConfiguration config, ILoggerFactory loggerFactory)
		{
			Config = config ?? throw new System.ArgumentNullException(nameof(config));
			_loggerFactory = loggerFactory ?? throw new System.ArgumentNullException(nameof(loggerFactory));

			var channelName = Config["StreamServices:Mixer:Channel"];
			var token = Config["StreamServices:Mixer:Token"];
			HasToken = !string.IsNullOrEmpty(token);
			ChatAuthKey = HasToken ? "zxc1234" : null;
			UserName = HasToken ? "TestUser" : null;

			ChatWebSocket = new SimulatedClientWebSocket(true, HasToken, CHAT_WELCOME);
			ConstellationWebSocket = new SimulatedClientWebSocket(false, HasToken, CONSTALLATION_WELCOME);

			_restClientMock = new Mock<IMixerRestClient>();
			_restClientMock.Setup(x => x.InitAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((false, 0, 0)));
			_restClientMock.Setup(x => x.GetChatAuthKeyAndEndpointsAsync())
				.Returns(Task.FromResult(new API.Chats { Authkey = ChatAuthKey, Endpoints = Endpoints }));
			_restClientMock.Setup(x => x.ChannelName).Returns(channelName);
			_restClientMock.Setup(x => x.ChannelId).Returns(ChannelInfo.Id);
			_restClientMock.Setup(x => x.UserName).Returns(UserName);
			_restClientMock.Setup(x => x.UserId).Returns(ChannelInfo.UserId);
			_restClientMock.Setup(x => x.HasToken).Returns(HasToken);
		}

		public IClientWebSocketProxy CreateClientWebSocket(bool isChat) => ( isChat ) ? ChatWebSocket : ConstellationWebSocket;
		public IJsonRpcWebSocket CreateJsonRpcWebSocket(ILogger logger, IEventParser parser) =>
			new JsonRpcWebSocket(new Mock<ILogger>().Object, this, Config, parser) { SendTimeout = TimeSpan.FromMilliseconds(500) };
		public IMixerChat CreateChat(IMixerRestClient client, IEventParser parser, CancellationToken shutdownRequest) =>
			new MixerChat(Config, _loggerFactory, this, _restClientMock.Object, parser, Cancel.Token);
		public IMixerConstellation CreateConstellation(IEventParser parser, CancellationToken shutdownRequest) =>
			new MixerConstellation(Config, _loggerFactory, this, parser, Cancel.Token);
		public IMixerRestClient CreateRestClient() => _restClientMock.Object;
	}
}
