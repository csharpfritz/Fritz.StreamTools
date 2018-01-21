using System;
using System.IO;
using System.Linq;
using Fritz.StreamTools.Hubs;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Fritz.StreamTools.TagHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fritz.StreamTools.StartupServices
{
	public static class ConfigureServices
	{

		private static ILogger Logger;

		public static void Execute(
			IServiceCollection services,
			IConfiguration Configuration,
			ILoggerFactory loggerFactory
		)
		{

			Logger = loggerFactory.CreateLogger("StartupServices");

			services.AddSingleton<Models.RundownRepository>();

			services.Configure<FollowerGoalConfiguration>(Configuration.GetSection("FollowerGoal"));

			ConfigureStreamingServices(services, Configuration);

			services.AddSingleton<FollowerClient>();

			ConfigureAspNetFeatures(services);

			services.AddSingleton<IConfigureOptions<SignalrTagHelperOptions>, ConfigureSignalrTagHelperOptions>();
			services.AddSingleton<SignalrTagHelperOptions>(cfg => cfg.GetService<IOptions<SignalrTagHelperOptions>>().Value);
		}

		private static void ConfigureStreamingServices(
			IServiceCollection services,
			IConfiguration Configuration
		)
		{

			var sp = services.BuildServiceProvider();

			ConfigureTwitch(services, Configuration, sp);

			ConfigureMixer(services, Configuration, sp);

			ConfigureFake(services, Configuration, sp);

			services.AddSingleton<StreamService>();

		}

		private static void ConfigureTwitch(IServiceCollection services, IConfiguration Configuration, ServiceProvider sp)
		{
			if (!string.IsNullOrEmpty(Configuration["StreamServices:Twitch:ClientId"]))
			{

				Logger.LogInformation("Configuring Twitch Service");
				var svc = new Services.TwitchService(Configuration, sp.GetService<ILoggerFactory>());
				services.AddSingleton<IHostedService>(svc);
				services.AddSingleton<IStreamService>(svc);

			} else {

				Logger.LogInformation("Skipping Twitch Configuration - missing StreamServices:Twitch:ClientId");

			}
		}

		private static void ConfigureMixer(IServiceCollection services, IConfiguration Configuration, ServiceProvider sp)
		{
			if (!string.IsNullOrEmpty(Configuration["StreamServices:Mixer:ClientId"]))
			{
				Logger.LogInformation("Configuring Mixer Service");
				var mxr = new MixerService(Configuration, sp.GetService<ILoggerFactory>());
				services.AddSingleton<IHostedService>(mxr);
				services.AddSingleton<IStreamService>(mxr);
			}
			else
			{

				Logger.LogInformation("Skipping Mixer Configuration - missing StreamServices:Mixer:ClientId");

			}
		}

		private static void ConfigureFake(IServiceCollection services, IConfiguration Configuration, ServiceProvider sp)
		{

			if (!bool.TryParse(Configuration["StreamServices:Fake:Enabled"], out var enabled) || !enabled) {

				// unable to parse the value, by default disable the FakeService
				// Exit now, we are not enabling the service

				Logger.LogInformation("Skipping FakeService Configuration - StreamServices:Fake:Enabled not set or set to false");
				return;

			}

			Logger.LogInformation("Configuring Fake Service");
			var mck = new FakeService(Configuration, sp.GetService<ILoggerFactory>());
			services.AddSingleton<IHostedService>(mck);
			services.AddSingleton<IStreamService>(mck);

		}

		private static void ConfigureAspNetFeatures(IServiceCollection services)
		{
			services.AddSignalR();
			services.AddSingleton<FollowerHub>();

			services.AddMvc();
		}


	}
}