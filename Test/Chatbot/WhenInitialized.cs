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
using Microsoft.Extensions.DependencyInjection;
using Fritz.StreamLib.Core;

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

	}
[Fact]
	public void ShouldReturnAChatCommand()
	{

	  var fake = new FakeChatService();
	  var config = new Moq.Mock<IConfiguration>();
	  
	  var loggingFactory = new Moq.Mock<ILoggerFactory>();
	  var logger = new Mock<ILogger>();
	  loggingFactory.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

	  var chatService = new Mock<IChatService>();

	
	  chatService.Raise(cs => cs.ChatMessage += null, args);


	  var chatservices = new List<IChatService>();
	  
	  var serviceProvider = new Mock<IServiceProvider>();
	  serviceProvider
		  .Setup(x => x.GetService(typeof(IEnumerable<IChatService>)))
		  .Returns(chatservices);
			
	  var serviceScope = new Mock<IServiceScope>();
	  serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

	  var serviceScopeFactory = new Mock<IServiceScopeFactory>();
	  serviceScopeFactory
		  .Setup(x => x.CreateScope())
		  .Returns(serviceScope.Object);

	  serviceProvider
		  .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
		  .Returns(serviceScopeFactory.Object);


	  var args = new ChatMessageEventArgs
	  {
		Message = "!skeet",
		IsModerator = false,
		IsOwner = false
	  };

	  //string[] quote = { "test quote" };
	  var sut = new FritzBot(config.Object, serviceProvider.Object, loggingFactory.Object);
	  

	  // Act


	}

	private void WhenInitialized_ChatMessage(object sender, ChatMessageEventArgs e)
	{
	  throw new NotImplementedException();
	}

	
  }

}
