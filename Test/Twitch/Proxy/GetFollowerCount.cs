using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fritz.Twitch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Test.Twitch.Proxy
{

	public class GetFollowerCount
	{
		private static readonly HttpClient _Client;
		private static readonly ConfigurationSettings _Settings;

		static GetFollowerCount()
		{

			_Client = new HttpClient();
			_Settings = new Fritz.Twitch.ConfigurationSettings
			{
				ChannelName = "csharpfritz",
				ClientId = "t7y5txan5q662t7zj7p3l4wlth8zhv",
				UserId = "96909659"
			};

		}

		public GetFollowerCount(ITestOutputHelper output)
		{
			Mockery = new MockRepository(MockBehavior.Loose);
			this.Output = output;
			this.Logger = new XUnitLogger(Output);
		}

		public MockRepository Mockery { get; }
		public ITestOutputHelper Output { get; }
		public XUnitLogger Logger { get; }

		[Fact]
		public async Task ShouldReturnNonZeroCount()
		{

			// Arrange
			var sut = new Fritz.Twitch.Proxy(_Client, _Settings, Logger);

			// Act
			var followerCount = await sut.GetFollowerCountAsync();
			Output.WriteLine($"csharpfritz Twitch follower count: {followerCount}");

			// Assert
			Assert.NotEqual(0, followerCount);

		}


		[Fact]
		public void ShouldParseFollowerResult()
		{

			var sampleData = @"{
				""total"": 12345,
				""data"": [],
				""pagination"":{
					""cursor"": ""eyJiIjpudWxsLCJhIjoiMTUwMzQ0MTc3NjQyNDQyMjAwMCJ9""
				}
			}";

			var count = Fritz.Twitch.Proxy.ParseFollowerResult(sampleData);
			Output.WriteLine($"Follower Count: {count}");

			Assert.Equal(12345, count);

		}


	}

	public class XUnitLogger : ILogger
	{

		public XUnitLogger(ITestOutputHelper outputHelper)
		{
			this.OutputHelper = outputHelper;
		}

		public ITestOutputHelper OutputHelper { get; }

		public IDisposable BeginScope<TState>(TState state)
		{
			// throw new NotImplementedException();
			return null;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{

			var logValues = state as FormattedLogValues;

			OutputHelper.WriteLine(logValues[0].Value.ToString());
			Debug.WriteLine(logValues[0].Value.ToString());

		}
	}

}
