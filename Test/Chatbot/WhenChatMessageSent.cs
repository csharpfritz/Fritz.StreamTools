using Fritz.Chatbot.Commands;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test.Chatbot
{
	public class WhenChatMessageRecieved
	{
		private readonly Mock<IConfiguration> _config;
		private readonly Mock<IChatService> _chatservice;
		private readonly Mock<IServiceProvider> _serviceProvider;
		private readonly Mock<ILogger> _logger;
		private readonly Mock<ILoggerFactory> _loggerFactory;

		public WhenChatMessageRecieved()
		{
			_loggerFactory = new Mock<ILoggerFactory>();
			_logger = new Mock<ILogger>();
			_loggerFactory.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
			_config = new Mock<IConfiguration>();
			_config.SetupGet(s => s[FritzBot.CONFIGURATION_ROOT + ":CooldownTime"]).Returns("1");
			_chatservice = new Mock<IChatService>();
			_chatservice.SetupGet(x => x.IsAuthenticated).Returns(true);

			var chatServices = new List<IChatService> { _chatservice.Object };
			_serviceProvider = new Mock<IServiceProvider>();
			_serviceProvider
						.Setup(provider => provider.GetService(typeof(IEnumerable<IChatService>)))
						.Returns(() => chatServices);

			var chatBasicCommands = new List<IBasicCommand>
			{
				new EchoCommand(),
				new SkeetCommand(),
				new HelpCommand(_serviceProvider.Object),
				new PingCommand()
			};

			_serviceProvider
						.Setup(provider => provider.GetService(typeof(IEnumerable<IBasicCommand>)))
						.Returns(() => chatBasicCommands);

			var chatExtendedCommands = new List<IExtendedCommand>()
			{
				new HttpPageTitleCommand()
			};

			_serviceProvider
						.Setup(provider => provider.GetService(typeof(IEnumerable<IExtendedCommand>)))
						.Returns(() => chatExtendedCommands);
		}

		[Fact]
		public void ShouldReturnPongMessage()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object, _loggerFactory.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "!ping",
				UserName = "testusername",
				IsModerator = false,
				IsOwner = false
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Verify(sm => sm.SendWhisperAsync(
																		It.IsAny<string>(),
																		It.Is<string>(x => x.StartsWith("pong"))),
																		Times.Once);
		}

		[Fact]
		public void ShouldReturnEchoMessage()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object, _loggerFactory.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "!echo Test Message",
				UserName = "testusername",
				IsModerator = false,
				IsOwner = false
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Verify(sm => sm.SendWhisperAsync(
						It.IsAny<string>(),
						It.Is<string>(x => x.StartsWith("Echo reply: Test Message"))),
						Times.Once);
		}

		[Fact]
		public void ShouldReturnHelpMessage()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object, _loggerFactory.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "!help",
				UserName = "testusername",
				IsModerator = false,
				IsOwner = false
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Verify(sm => sm.SendMessageAsync(
						It.Is<string>(x => x.StartsWith("Supported commands:"))),
						Times.Once);

			_chatservice.Verify(sm => sm.SendMessageAsync(
						It.Is<string>(x => x.Contains("!help"))),
						Times.Once);
		}

		[Fact]
		public void ShouldReturnHelpSkeetMessage()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object, _loggerFactory.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var command = "skeet";
			var args = new ChatMessageEventArgs
			{
				Message = $"!help {command}",
				UserName = "testusername",
				IsModerator = false,
				IsOwner = false
			};

			var description = new SkeetCommand().Description;

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Verify(sm => sm.SendMessageAsync(
						It.Is<string>(x => x.StartsWith($"{command}: {description}"))),
						Times.Once);
		}

		[Fact]
		public void ShouldLogIfCommandsToFast()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object, _loggerFactory.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "!help",
				ServiceName = "TestService",
				UserName = "testusername",
				IsModerator = false,
				IsOwner = false,
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Raise(cs => cs.ChatMessage += null, args);

			const string expectWarning = "Ignoring command help from testusername on TestService. Cooldown active";
			_logger.Verify(
						m => m.Log(
									 LogLevel.Warning,
									 It.IsAny<EventId>(),
									 It.Is<FormattedLogValues>(v => v.ToString().Contains(expectWarning)),
									 It.IsAny<Exception>(),
									 It.IsAny<Func<object, Exception, string>>())
			);
		}

		[Fact]
		public void ShouldIgnoreCommandsToFastIfModerator()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object, _loggerFactory.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "!help",
				ServiceName = "TestService",
				UserName = "Moderator",
				IsModerator = true,
				IsOwner = false
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			
			var verifyTimes = Moq.Times.Once();

#if !DEBUG
			verifyTimes = Moq.Times.Exactly(2);
#endif

	  	_chatservice.Verify(sm => sm.SendMessageAsync(
					It.Is<string>(x => x.StartsWith("Supported commands:"))),
					verifyTimes);
		}

		[Fact]
		public void ShouldReturnLinkTitle()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object, _loggerFactory.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "Hey! Check this link: www.google.com",
				UserName = "testusername",
				IsModerator = false,
				IsOwner = false
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Verify(sm => sm.SendMessageAsync(
							It.Is<string>(x => x.StartsWith("testusername's linked page title:"))),
							Times.Once);
		}

		[Fact]
		public void ShouldReturnTwoLinkTitles()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object, _loggerFactory.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "Which search engine is better? www.google.com or www.bing.com",
				UserName = "testusername",
				IsModerator = false,
				IsOwner = false
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);

			_chatservice.Verify(sm => sm.SendMessageAsync(
							It.Is<string>(x => x.StartsWith("testusername's linked page title:"))),
							Times.Exactly(2));
		}
	}
}
