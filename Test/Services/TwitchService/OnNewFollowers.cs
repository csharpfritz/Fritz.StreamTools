using Fritz.StreamTools.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Interfaces;
using Xunit;
using FRITZ = Fritz.StreamTools.Services;

namespace Test.Services.TwitchService
{

	public class OnNewFollowers : BaseFixture
	{
		private const int initialFollowers = 10;

		public OnNewFollowers()
		{
			CreateLogger();
			this.MockConfiguration = Mockery.Create<IConfiguration>();

		}

		private void CreateLogger()
		{
			MockLoggerFactory = Mockery.Create<ILoggerFactory>();
			MockLogger = Mockery.Create<ILogger>();

			MockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);

		}

		private Mock<ILoggerFactory> MockLoggerFactory { get; set; }
		private Mock<ILogger> MockLogger { get; set; }
		public Mock<IConfiguration> MockConfiguration { get; set; }

		[Fact]
		public void ShouldSetCurrentFollowerCount()
		{

			// arrange
			var newFollowerCount = new Random().Next(400, 500);
			var newFollowerList = new List<IFollow>(new IFollow[newFollowerCount]);
			var args = new TwitchLib.Events.Services.FollowerService.OnNewFollowersDetectedArgs
			{
				NewFollowers = newFollowerList
			};
			// MockLogger.Setup(l => LoggerExtensions.LogInformation(l, It.IsAny<string>()))
			// 	.Callback<string>(msg => Console.Out.WriteLine(msg));

			// act
			var sut = new FRITZ.TwitchService(MockConfiguration.Object, MockLoggerFactory.Object)
			{
				CurrentFollowerCount = initialFollowers
			};
			sut.Service_OnNewFollowersDetected(null, args);

			// assert
			Assert.Equal(newFollowerCount + initialFollowers, sut.CurrentFollowerCount);

		}

		[Fact]
		public void ShouldRaiseEventProperly()
		{

			// arrange
			var newFollowerCount = new Random().Next(400, 500);
			var newFollowerList = new List<IFollow>(new IFollow[newFollowerCount]);
			var myArgs = new TwitchLib.Events.Services.FollowerService.OnNewFollowersDetectedArgs
			{
				NewFollowers = newFollowerList
			};

			// act
			var sut = new FRITZ.TwitchService(MockConfiguration.Object, MockLoggerFactory.Object)
			{
				CurrentFollowerCount = initialFollowers
			};

			// arrange
			var evt = Assert.Raises<ServiceUpdatedEventArgs>(
				h => sut.Updated += h,
				h => sut.Updated -= h,
				() => sut.Service_OnNewFollowersDetected(null, myArgs)
			);

			Assert.Equal(initialFollowers + newFollowerCount, evt.Arguments.NewFollowers);
			Assert.Null(evt.Arguments.NewViewers);

		}

	}

}