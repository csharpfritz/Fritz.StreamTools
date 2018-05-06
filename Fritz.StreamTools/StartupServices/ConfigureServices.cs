using System;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Fritz.StreamTools.TagHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace Fritz.StreamTools.StartupServices
{
  public static class ConfigureServices
	{
		public static void Execute(
			IServiceCollection services,
			IConfiguration configuration)
		{
			services.AddSingleton<RundownRepository>();
			services.Configure<FollowerGoalConfiguration>(configuration.GetSection("FollowerGoal"));
			services.Configure<FollowerCountConfiguration>(configuration.GetSection("FollowerCount"));
			services.Configure<GitHubConfiguration>(configuration.GetSection("GitHub"));
			services.AddStreamingServices(configuration);
			services.AddSingleton<FollowerClient>();
			services.AddAspNetFeatures();

			services.AddSingleton<IConfigureOptions<SignalrTagHelperOptions>, ConfigureSignalrTagHelperOptions>();
			services.AddSingleton<SignalrTagHelperOptions>(cfg => cfg.GetService<IOptions<SignalrTagHelperOptions>>().Value);

			services.AddSingleton<IHostedService, FritzBot>();

			services.AddSingleton(new GitHubClient(new ProductHeaderValue("Fritz.StreamTools")));
		}

		private static void AddStreamingServices(this IServiceCollection services,
			IConfiguration configuration)
		{

			services.Configure<Twitch.ConfigurationSettings>(configuration.GetSection("StreamServices:Twitch"));

			var provider = services.BuildServiceProvider();

			services.AddStreamService<TwitchService>(configuration,
				(c, l) => new TwitchService(c, l, provider.GetService<Fritz.Twitch.Proxy>(), provider.GetService<Fritz.Twitch.ChatClient>()),
				c => string.IsNullOrEmpty(c["StreamServices:Twitch:ClientId"]));		// Test to disable
			services.AddStreamService(configuration, 
				(c, l) => new MixerService(c, l),                                   // Factory
				c => string.IsNullOrEmpty(c["StreamServices:Mixer:Channel"]));			// Test to disable
			services.AddStreamService(configuration, 
				(c, l) => new FakeService(c, l),                                                          // Factory
				c => !bool.TryParse(c["StreamServices:Fake:Enabled"], out var enabled) || !enabled);			// Test to disable
			
			services.AddSingleton<StreamService>();	
		}

		/// <summary>
		/// Generically configure stream service providers and register them with the DI container
		/// </summary>
		/// <typeparam name="TStreamService">Type of StreamService to create</typeparam>
		/// <param name="services">The DI services configuration object</param>
		/// <param name="configuration">Application Configuration to use to populate our service</param>
		/// <param name="factory">Callback method that defines how to instantiate the service</param>
		/// <param name="isDisabled">Callback test to determine whether to disable the service</param>
		private static void AddStreamService<TStreamService>(this IServiceCollection services,
			IConfiguration configuration,
			Func<IConfiguration, ILoggerFactory, TStreamService> factory,
			Func<IConfiguration, bool> isDisabled)
			where TStreamService : class, IStreamService
		{

			// Don't configure this service if it is disabled
			if (isDisabled(configuration))
			{
				return;
			}

			// Configure and grab a logger so that we can log information
			// about the creation of the services
			var provider = services.BuildServiceProvider();   // Build a 'temporary' instance of the DI container
			var loggerFactory = provider.GetService<ILoggerFactory>();

			var service = factory(configuration, loggerFactory);

			services.AddSingleton(service as IHostedService);
			services.AddSingleton(service as IStreamService);
			services.AddSingleton(service);

			if (service is IChatService chatService)
			{
				services.AddSingleton(chatService);
			}
		}

		/// <summary>
		/// Configure the standard ASP.NET Core services
		/// </summary>
		/// <param name="services"></param>
		private static void AddAspNetFeatures(this IServiceCollection services)
		{
			services.AddSignalR();
			services.AddSingleton<FollowerHub>();
			services.AddMvc();
		}

	}
}
