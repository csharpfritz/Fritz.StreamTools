using Fritz.Chatbot.Commands;
using Fritz.StreamTools.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Test.Chatbot
{

  public class WhenInitialized
  {
	private readonly MockRepository _Mockery;
	private readonly Random _Random;
	private IConfiguration _Config;
	private ILoggerFactory _LoggerFactory;

	public WhenInitialized()
	{

	  _Mockery = new MockRepository(MockBehavior.Loose);
	  _Random = new Random(DateTime.Now.Second);

	  SetupMocks();

	}

	private void SetupMocks()
	{
	  var config = _Mockery.Create<IConfiguration>();
	  config.SetupGet(c => c[FritzBot.CONFIGURATION_ROOT + ":CooldownTime"]).Returns(TimeSpan.FromSeconds(_Random.Next(60)).ToString());
	  _Config = config.Object;

	  var loggerFactory = _Mockery.Create<ILoggerFactory>();
	  loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(_Mockery.Create<ILogger>().Object);
	  _LoggerFactory = loggerFactory.Object;

	}

	[Fact]
	public void ShouldConfigureCooldown()
	{

	  // arrange


	  // act
	  var sut = new FritzBot();
	  sut.Initialize(_Config, null, _LoggerFactory);

	  // assert
	  Assert.Equal(_Config[FritzBot.CONFIGURATION_ROOT + ":CooldownTime"], sut.CooldownTime.ToString());

	}

	[Fact]
	public void ShouldRegisterCommands()
	{

	  // Arrange

	  // Act
	  var sut = new FritzBot();
	  sut.Initialize(_Config, null, _LoggerFactory);

	  // Assert
	  Assert.NotEmpty(FritzBot._CommandRegistry);

	}

		 protected static void CallSync(Action target)
    {
        var task = new Task(target);
        task.RunSynchronously();
    }
	[Fact]
	public void ShouldReturnARandomSkeetQuoteAsync()
	{
	  string[] quote = { "test quote" };
	  var sut = new SkeetCommand(quote);
		var fake = new FakeChatService();
	  sut.ChatService = fake;
	  // Arrange
	  Task.WaitAll(sut.Execute("", ""));
	  
	  // Act

	  Assert.Equal(quote[0], fake.MessageSent);
	  ///await sut.Execute("username", "");






	}


  }

}
