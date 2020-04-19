using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fritz.Chatbot.Commands;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Services;
using Fritz.StreamTools.StartupServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
							}).Build();

			var serviceCollection = new ServiceCollection();
			serviceCollection.AddSingleton<IConfiguration>(configuration);
			serviceCollection.AddSingleton<ILogger>(NullLogger.Instance);
			serviceCollection.AddSingleton<IHostEnvironment>(new StubHostEnvironment());


			var serviceRequriedConfiguration = new Dictionary<Type, string[]>()
			{
				{ typeof(FakeConfigurationRequiredService), new [] { "FakeConfiguration:PropertyOne", "FakeConfiguration:PropertyTwo", }}
			};

			ConfigureServices.Execute(serviceCollection, configuration, serviceRequriedConfiguration);

			var provider = serviceCollection.BuildServiceProvider();
			Assert.Contains(provider.GetServices<IHostedService>().Select(x => x.GetType()), type => type == typeof(FakeConfigurationRequiredService));
	}

		[Fact]
		public void Execute_ShouldSkipRegisterServices_IfAnyOfRequiredConfigurationNotPass()
		{
			var configuration = new ConfigurationBuilder().AddInMemoryCollection(
								new Dictionary<string, string>()
								{
									{ "FakeConfiguration:PropertyOne", "RandomValue" },
								}).Build();

			var serviceCollection = new ServiceCollection();
			serviceCollection.AddSingleton<IConfiguration>(configuration);
			serviceCollection.AddSingleton<ILogger>(NullLogger.Instance);
			serviceCollection.AddSingleton<IHostEnvironment>(new StubHostEnvironment());


			var serviceRequriedConfiguration = new Dictionary<Type, string[]>()
				{
					{ typeof(FakeConfigurationRequiredService), new [] { "FakeConfiguration:PropertyOne", "MissingFakeConfiguration:MissingPropertyTwo", }}
				};

			ConfigureServices.Execute(serviceCollection, configuration, serviceRequriedConfiguration);

			var provider = serviceCollection.BuildServiceProvider();
			Assert.DoesNotContain(provider.GetServices<IHostedService>().Select(x => x.GetType()), type => type == typeof(FakeConfigurationRequiredService));
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
			serviceCollection.AddSingleton<ILogger>(NullLogger.Instance);
			serviceCollection.AddSingleton<IHostEnvironment>(new StubHostEnvironment());

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

		private class FakeConfigurationRequiredService : IHostedService
		{
			public Task StartAsync(CancellationToken cancellationToken)
			{
				return null;
			}

			public Task StopAsync(CancellationToken cancellationToken)
			{
			return null;
			}
		}
  }

	internal class StubHostEnvironment : IHostEnvironment
	{
		public StubHostEnvironment()
		{
		}

		public string EnvironmentName { get; set; }
		public string ApplicationName { get; set; }
		public string ContentRootPath { get; set; }
		public IFileProvider ContentRootFileProvider { get; set; }
	}
}
