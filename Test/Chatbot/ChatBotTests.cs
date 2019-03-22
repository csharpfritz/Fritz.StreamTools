using Fritz.Chatbot;
using Fritz.Chatbot.Commands;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test.Chatbot
{
	public class ChatBotTests
	{
		private readonly Mock<IConfiguration> _config;
		private readonly Mock<IServiceProvider> _serviceProvider;
		private readonly Mock<IChatService> _chatservice;

		private readonly FakeBasicCommand _basicCommand;
		private readonly FakeBasicCommand _basicCommandWithCooldown;
		private readonly FakeBasicCommand _finalBasicCommandWithExtendedCommandPattern;
		private readonly FakeBasicCommand _nonFinalBasicCommandWithExtendedCommandPatter;

		private readonly FakeExtendedCommand _extendedCommand;
		private readonly FakeExtendedCommand _nonFinalExtendedCommand;
		private readonly FakeExtendedCommand _finalExtendedCommand;

		public ChatBotTests()
		{
			_config = new Mock<IConfiguration>();
			_chatservice = new Mock<IChatService>();
			_chatservice.SetupGet(x => x.IsAuthenticated).Returns(true);
			_chatservice.SetupGet(s => s.BotUserName).Returns("MockedChatSerivce");

			var chatServices = new List<IChatService> { _chatservice.Object };
			_serviceProvider = new Mock<IServiceProvider>();
			_serviceProvider
				.Setup(provider => provider.GetService(typeof(IEnumerable<IChatService>)))
				.Returns(() => chatServices);

			_basicCommand = new FakeBasicCommand("FakeBasicCommand");
			_basicCommandWithCooldown = new FakeBasicCommand("FakeCooldownBasicCommand", new TimeSpan(0, 0, 10));
			_finalBasicCommandWithExtendedCommandPattern = new FakeBasicCommand("FakeExtendedCommandWithFinal");
			_nonFinalBasicCommandWithExtendedCommandPatter = new FakeBasicCommand("FakeExtendedCommandWithoutFinal");
			var chatBasicCommands = new List<IBasicCommand>
			{
				_basicCommand,
				_basicCommandWithCooldown,
				_finalBasicCommandWithExtendedCommandPattern,
				_nonFinalBasicCommandWithExtendedCommandPatter
			};

			_serviceProvider
				.Setup(provider => provider.GetService(typeof(IEnumerable<IBasicCommand>)))
				.Returns(() => chatBasicCommands);

			_extendedCommand = new FakeExtendedCommand("FakeExtendedCommand", order: 999);
			_nonFinalExtendedCommand = new FakeExtendedCommand("FakeExtendedCommandWithoutFinal", final: false);
			_finalExtendedCommand = new FakeExtendedCommand("FakeExtendedCommandWithFinal", final: true);
			var chatExtendedCommands = new List<IExtendedCommand>()
			{
				_extendedCommand,
				_finalExtendedCommand,
				_nonFinalExtendedCommand
			};

			_serviceProvider
				.Setup(provider => provider.GetService(typeof(IEnumerable<IExtendedCommand>)))
				.Returns(() => chatExtendedCommands);
		}

		[Fact]
		public void ShouldInvokeBasicCommandManyTimesWithoutCooldown()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "!FakeBasicCommand",
				UserName = "TestUser"
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Verify(sm => sm.SendWhisperAsync(
					It.IsAny<string>(),
					It.Is<string>(x => x.Equals(_basicCommand.ExecutedAnswer))),
					Times.Exactly(2));
		}

		[Fact]
		public void ShouldInvokeExtendedCommand()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "FakeExtendedCommand",
				UserName = "TestUser"
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Verify(sm => sm.SendWhisperAsync(
					It.IsAny<string>(),
					It.Is<string>(x => x.Equals(_extendedCommand.ExecutedAnswer))),
					Times.Once);
		}

		[Fact]
		public void ShouldReturnInfoWhenTryInvokeNonRegisterCommand()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "!SpecialNonExistingCommand",
				UserName = "TestUser"
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Verify(sm => sm.SendWhisperAsync(
					It.IsAny<string>(),
					It.Is<string>(x => x.Equals(FritzBot.UnknownCommandMessage))),
					Times.Once);
		}

		[Fact]
		public void ShouldSetDefaultCooldownTimeIsMissingInConfiguration()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			Assert.Equal(TimeSpan.Zero, sut.CooldownTime);
		}

		[Fact]
		public void ShouldIgnoreCommandSendBeforeEndOfCooldown()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "!FakeCooldownBasicCommand",
				UserName = "TestUser"
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Verify(sm => sm.SendWhisperAsync(
					It.IsAny<string>(),
					It.Is<string>(x => x.Equals(_basicCommandWithCooldown.ExecutedAnswer))),
					Times.Once);
		}

		[Fact]
		public void ShouldIgnoreCooldownIfSendByModerator()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "!FakeCooldownBasicCommand",
				UserName = "ModeratorTestUser",
				IsModerator = true
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Verify(sm => sm.SendWhisperAsync(
					It.IsAny<string>(),
					It.Is<string>(x => x.Equals(_basicCommandWithCooldown.ExecutedAnswer))),
					Times.Exactly(2));
		}

		[Fact]
		public void ShouldInovkeBothExtendedCommandAndBasicCommandPatternAlsoMatch()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "!FakeExtendedCommandWithoutFinal",
				UserName = "TestUser"
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Verify(sm => sm.SendWhisperAsync(
					It.IsAny<string>(),
					It.Is<string>(x => x.Equals(_nonFinalBasicCommandWithExtendedCommandPatter.ExecutedAnswer))),
					Times.Once);
			_chatservice.Verify(sm => sm.SendWhisperAsync(
					It.IsAny<string>(),
					It.Is<string>(x => x.Equals(_nonFinalExtendedCommand.ExecutedAnswer))),
					Times.Once);
		}

		[Fact]
		public void ShouldInovkeOnlyFinalExtendedCommandIfBasicCommandPatternAlsoMatch()
		{
			var sut = new FritzBot(_config.Object, _serviceProvider.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			var args = new ChatMessageEventArgs
			{
				Message = "!FakeExtendedCommandWithFinal",
				UserName = "TestUser"
			};

			_chatservice.Raise(cs => cs.ChatMessage += null, args);
			_chatservice.Verify(sm => sm.SendWhisperAsync(
					It.IsAny<string>(),
					It.Is<string>(x => x.Equals(_finalBasicCommandWithExtendedCommandPattern.ExecutedAnswer))),
					Times.Never);
			_chatservice.Verify(sm => sm.SendWhisperAsync(
					It.IsAny<string>(),
					It.Is<string>(x => x.Equals(_finalExtendedCommand.ExecutedAnswer))),
					Times.Once);
		}

		[Theory]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(3)]
		public void ShouldSetCooldownTimeWhenSetInConfiguration(int cooldownTime)
		{
			_config.SetupGet(s => s[FritzBot.ConfigurationRoot + ":CooldownTime"]).Returns(cooldownTime.ToString());

			var sut = new FritzBot(_config.Object, _serviceProvider.Object);
			Task.WaitAll(sut.StartAsync(new CancellationToken()));

			Assert.Equal(TimeSpan.Parse(cooldownTime.ToString()), sut.CooldownTime);
		}

		[Fact]
		public void ThrowExceptionWhenConfigurationIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new FritzBot(null, _serviceProvider.Object));
		}

		[Fact]
		public void ThrowExceptionWhenServiceProviderIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new FritzBot(_config.Object, null));
		}

		private class FakeBasicCommand : IBasicCommand
		{
			public string Trigger { get; }
			public string Description => "Fake basic command onlt for unit tests.";
			public TimeSpan? Cooldown { get; }

			public string ExecutedAnswer => $"Executed command for trigger: {Trigger}";
			public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
			{
				await chatService.SendWhisperAsync(userName, ExecutedAnswer);
			}

			public FakeBasicCommand(string trigger, TimeSpan? cooldown = null)
			{
				Trigger = trigger;
				Cooldown = cooldown;
			}
		}

		private class FakeExtendedCommand : IExtendedCommand
		{
			public string Name { get; }
			public string Description => "Fake extended command only for unit tests.";
			public int Order { get; }
			public bool Final { get; }
			public TimeSpan? Cooldown => null;

			public bool CanExecute(string userName, string fullCommandText)
			{
				return fullCommandText.Contains(Name);
			}

			public string ExecutedAnswer => $"ExtendedCommandExcuted Name:{ Name }";
			public async Task Execute(IChatService chatService, string userName, string fullCommandText)
			{
				await chatService.SendWhisperAsync(userName, ExecutedAnswer);
			}

			public FakeExtendedCommand(string name, int order = 0, bool final = false)
			{
				Name = name;
				Order = order;
				Final = final;
			}
		}
	}
}
