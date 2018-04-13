using Fritz.Chatbot.Commands;
using Fritz.StreamTools.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Fritz.StreamLib.Core;
using System.Threading;
using Microsoft.Extensions.Logging.Internal;

namespace Test.Chatbot
{
	public class WhenChatMessageRecieved
	{
		private Mock<IConfiguration> _config;
		private Mock<IChatService> _chatservice;
		private Mock<IServiceProvider> _serviceProvider;
		private Mock<ILogger> _logger;
		private Mock<ILoggerFactory> _loggerFactory;
		public WhenChatMessageRecieved()

		{
			_loggerFactory = new Moq.Mock<ILoggerFactory>();
			_logger = new Moq.Mock<ILogger>();
			_loggerFactory.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
			_config = new Moq.Mock<IConfiguration>();
			_config.SetupGet(s => s[FritzBot.CONFIGURATION_ROOT + ":CooldownTime"]).Returns("1");
			_chatservice = new Mock<IChatService>();
			_chatservice.SetupGet(x => x.IsAuthenticated).Returns(true);

			var chatServices = new List<IChatService> { _chatservice.Object };
			_serviceProvider = new Mock<IServiceProvider>();
			_serviceProvider
						.Setup(provider => provider.GetService(typeof(IEnumerable<IChatService>)))
						.Returns(() => chatServices);
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
																							 It.Is<string>(x => x.StartsWith("pong"))), Times.AtLeastOnce);
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
						It.IsAny<string>()
						, It.Is<string>(x => x.StartsWith("Echo reply: Test Message"))), Times.AtLeastOnce);
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
						It.Is<string>(x => x.Contains("Supported commands: "))), Times.AtLeastOnce);
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
						It.Is<string>(x => x.StartsWith($"{command}: {description}"))), Times.AtLeastOnce);
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
									 It.IsAny<Func<object, Exception, string>>()
						)

			);
		}
		[Fact]
		public void ShouldIgnoreCommandsToFastIfModeratorx()
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

			_chatservice.Verify(sm => sm.SendMessageAsync(
						It.Is<string>(x => x.Contains("Supported commands: !"))), Times.Exactly(2));
		}

	[Fact]
	public void ShouldReturnGithubCommand()
	{
	  var sut = new FritzBot(_config.Object, _serviceProvider.Object, _loggerFactory.Object);
	  Task.WaitAll(sut.StartAsync(new CancellationToken()));
	  var args = new ChatMessageEventArgs
	  {
		Message = "!github",
		UserName = "testusername",
		IsModerator = false,
		IsOwner = false
	  };

	  _chatservice.Raise(cs => cs.ChatMessage += null, args);
	  _chatservice.Verify(sm => sm.SendMessageAsync(
						It.Is<string>(x => x.Contains("Jeff's Github repository can by found here: https://github.com/csharpfritz/"))), Times.Exactly(1));
	}

  }
}
