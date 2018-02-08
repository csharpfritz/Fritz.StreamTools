using System;
using System.Collections.Generic;
using System.Linq;
using Fritz.StreamTools.Services;
using Fritz.StreamTools.StartupServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace StreamToolsTests.Startup
{
	
	public class ConfigureServicesTests
	{
		[Theory, MemberData(nameof(Configurations))]
		public void Execute_RegisterStreamServicesWithVariousConfigurations_ReturnExpected(Dictionary<string, string> configurations, Type[] expected)
		{
			// arrange
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(configurations)
				.Build();

			var serviceCollection = new ServiceCollection();
			serviceCollection.AddSingleton<ILoggerFactory>(new LoggerFactory());
				
			// act
			ConfigureServices.Execute(serviceCollection, configuration);

			// assert
			var provider = serviceCollection.BuildServiceProvider();

			Assert.Equal(expected, provider.GetServices<IHostedService>().Select(x => x.GetType()));
			Assert.Equal(expected, provider.GetServices<IStreamService>().Select(x => x.GetType()));
		}

		public static IEnumerable<object[]> Configurations
		{
			get
			{
				yield return new object[]{ MakeFakeConfiguration("123456", "654321", true), new [] { typeof(TwitchService), typeof(MixerService), typeof(FakeService) } };
				yield return new object[]{ MakeFakeConfiguration("", "654321", true), new [] { typeof(MixerService), typeof(FakeService) } };
				yield return new object[]{ MakeFakeConfiguration("", "", true), new [] { typeof(FakeService) } };
				yield return new object[]{ MakeFakeConfiguration("123456", "654321", false), new [] { typeof(TwitchService), typeof(MixerService) } };
			}
		}

		private static Dictionary<string, string> MakeFakeConfiguration(string twitchClientId,
			string mixerClientId,
			bool enableFake)
		{
			return new Dictionary<string, string>
			{
				{"StreamServices:Twitch:ClientId", twitchClientId},
				{"StreamServices:Mixer:ClientId", mixerClientId},
				{"StreamServices:Fake:Enabled", enableFake.ToString()}
			};
		}
	}
}