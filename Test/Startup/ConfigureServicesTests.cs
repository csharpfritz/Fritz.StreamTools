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

namespace Test.Startup
{
	
	public class ConfigureServicesTests
	{
		[Theory]
		[InlineData("StreamServices:Twitch:ClientId", "123456", typeof(TwitchService))]
		[InlineData("StreamServices:Mixer:ClientId", "654321", typeof(MixerService))]
		[InlineData("StreamServices:Fake:Enabled", "true", typeof(FakeService))]
		public void Execute_RegisterStreamService_ReturnExpected(string configurationKey, string configurationValue, Type expected)
		{
			// arrange
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string> { { configurationKey, configurationValue } })
				.Build();

			var serviceCollection = new ServiceCollection();
			serviceCollection.AddSingleton<ILoggerFactory>(new LoggerFactory());
				
			// act
			ConfigureServices.Execute(serviceCollection, configuration);

			// assert
			var provider = serviceCollection.BuildServiceProvider();
			var expectedHostedServices = provider
				.GetServices<IHostedService>();
			var expectedStreamServices = provider
				.GetServices<IStreamService>();
			
			Assert.IsType(expected, expectedHostedServices.Single());
			Assert.IsType(expected, expectedStreamServices.Single());
		}
	}
}