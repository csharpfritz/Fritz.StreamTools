using Fritz.StreamTools.Hubs;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

		}

		private static void ConfigureStreamingServices(
			IServiceCollection services,
			IConfiguration Configuration
		)
		{

			var sp = services.BuildServiceProvider();

			ConfigureTwitch(services, Configuration, sp);

			ConfigureMixer(services, Configuration, sp);

			ConfigureMock(services, Configuration, sp);

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

		private static void ConfigureMock(IServiceCollection services, IConfiguration Configuration, ServiceProvider sp)
		{
			if (Configuration["StreamServices:Mock:Switch"] == "on") {
				var mck = new MockService(Configuration, sp.GetService<ILoggerFactory>());
				services.AddSingleton<IHostedService>(mck);
				services.AddSingleton<IStreamService>(mck);
			}
		}

		private static void ConfigureAspNetFeatures(IServiceCollection services)
		{
			services.AddSignalR();
			services.AddSingleton<FollowerHub>();

			services.AddMvc();
		}


	}
}
