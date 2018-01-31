using Fritz.StreamTools.Hubs;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

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
			services.ConfigureStreamingServices(configuration);
			services.AddSingleton<FollowerClient>();
			services.ConfigureAspNetFeatures();
		}

		private static void ConfigureStreamingServices(this IServiceCollection services,
			IConfiguration configuration)
		{		
			services.ConfigureStreamService(configuration, 
				(c, l) => new TwitchService(c, l), 
				c => string.IsNullOrEmpty(c["StreamServices:Twitch:ClientId"]));
			services.ConfigureStreamService(configuration, 
				(c, l) => new MixerService(c, l), 
				c => string.IsNullOrEmpty(c["StreamServices:Mixer:ClientId"]));
			services.ConfigureStreamService(configuration, 
				(c, l) => new FakeService(c, l), 
				c => !bool.TryParse(c["StreamServices:Fake:Enabled"], out var enabled) || !enabled);
			
			services.AddSingleton<StreamService>();	
		}

		private static void ConfigureStreamService<TStreamService>(this IServiceCollection services, 
			IConfiguration configuration, Func<IConfiguration, ILoggerFactory, TStreamService> factory, 
			Func<IConfiguration, bool> isDisabled) 
			where TStreamService : class
		{
			if(isDisabled(configuration))
				return;
			
			var provider = services.BuildServiceProvider();
			var loggerFactory = provider.GetService<ILoggerFactory>();

			var service = factory(configuration, loggerFactory);
			
			services.AddSingleton(service as IHostedService);
			services.AddSingleton(service as IStreamService);
		}

		private static void ConfigureAspNetFeatures(this IServiceCollection services)
		{
			services.AddSignalR();
			services.AddSingleton<FollowerHub>();
			services.AddMvc();
		}
	}
}