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

		public static void Execute(
			IServiceCollection services,
			IConfiguration Configuration
		)
		{

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

				var svc = new Services.TwitchService(Configuration, sp.GetService<ILoggerFactory>());
				services.AddSingleton<IHostedService>(svc);
				services.AddSingleton<IStreamService>(svc);

			}
		}

		private static void ConfigureMixer(IServiceCollection services, IConfiguration Configuration, ServiceProvider sp)
		{
			if (!string.IsNullOrEmpty(Configuration["StreamServices:Mixer:ClientId"]))
			{
				var mxr = new MixerService(Configuration, sp.GetService<ILoggerFactory>());
				services.AddSingleton<IHostedService>(mxr);
				services.AddSingleton<IStreamService>(mxr);
			}
		}

		private static void ConfigureFake(IServiceCollection services, IConfiguration Configuration, ServiceProvider sp)
		{

			if (bool.TryParse(Configuration["StreamServices:Fake:Enabled"], out var enabled)) {

				if (!enabled)
				{
					// Exit now, we are not enabling the service
					return;
				}

			} else {

				// unable to parse the value, by default disable the FakeService
				return;

			}

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