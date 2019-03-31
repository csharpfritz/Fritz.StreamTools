using System;
using System.Collections.Generic;
using System.Linq;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Services;
using Fritz.StreamTools.StartupServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Test.Startup
{
  public class ConfigureServicesTests
	{
		[Fact]
		public void Execute_ShouldRegitserService_WhenAllRequiredConfigurationDone()
		{
			var configuration = new ConfigurationBuilder().AddInMemoryCollection(
							new Dictionary<string, string>()
							{
								{ "FakeConfiguration:PropertyOne", "RandomValue" },
								{ "FakeConfiguration:PropertyTwo", "RandomValue" }
							});

			var serviceCollection = new ServiceCollection();
			var serviceRequriedConfiguration = new Dictionary<Type, string[]>()
			{
				{ typeof(FakeConfigurationRequiredService), new [] { "FakeConfiguration:PropertyOne", "FakeConfiguration:PropertyTwo", }}
			};

			ConfigureServices.Execute(serviceCollection, configuration.Build(), serviceRequriedConfiguration);

			var provider = serviceCollection.BuildServiceProvider();
			Assert.Equal(typeof(FakeConfigurationRequiredService), provider.GetServices<FakeConfigurationRequiredService>().Select(x => x.GetType()).SingleOrDefault());
	}

		[Fact]
		public void Execute_ShouldSkipRegisterServices_IfAnyOfRequiredConfigurationNotPass()
		{
			var configuration = new ConfigurationBuilder().AddInMemoryCollection(
								new Dictionary<string, string>()
								{
									{ "FakeConfiguration:PropertyOne", "RandomValue" },
								});

			var serviceCollection = new ServiceCollection();
			var serviceRequriedConfiguration = new Dictionary<Type, string[]>()
				{
					{ typeof(FakeConfigurationRequiredService), new [] { "FakeConfiguration:PropertyOne", "MissingFakeConfiguration:MissingPropertyTwo", }}
				};

			ConfigureServices.Execute(serviceCollection, configuration.Build(), serviceRequriedConfiguration);

			var provider = serviceCollection.BuildServiceProvider();
			Assert.Null(provider.GetServices<FakeConfigurationRequiredService>().Select(x => x.GetType()).SingleOrDefault());
		}

		[Theory, MemberData(nameof(Configurations))]
		public void Execute_RegisterStreamServicesWithVariousConfigurations_ReturnExpected(Dictionary<string, string> configurations, Type[] expected)
		{
			// arrange
			var configuration = new ConfigurationBuilder().AddInMemoryCollection(configurations)
																										.Build();

			var serviceCollection = new ServiceCollection();
			serviceCollection.AddSingleton<ILoggerFactory>(new LoggerFactory());
			serviceCollection.AddSingleton<IConfiguration>(configuration);
				
			// act
			ConfigureServices.Execute(serviceCollection, configuration, new Dictionary<Type, string[]>());

			// assert
			var provider = serviceCollection.BuildServiceProvider();

			Assert.Equal(expected, provider.GetServices<IHostedService>().Select(x => x.GetType()).Intersect(expected));
			Assert.Equal(expected, provider.GetServices<IStreamService>().Select(x => x.GetType()).Intersect(expected));
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

		private static Dictionary<string, string> MakeFakeConfiguration(string twitchClientId, string mixerClientId, bool enableFake)
		{
			return new Dictionary<string, string>
			{
				{"StreamServices:Twitch:ClientId", twitchClientId},
				{"StreamServices:Mixer:Channel", mixerClientId},
				{"StreamServices:Fake:Enabled", enableFake.ToString()},
				{"FritzBot:ServerUrl", "http://localhost:80" }
			};
		}

		private class FakeConfigurationRequiredService { }
	}
}
