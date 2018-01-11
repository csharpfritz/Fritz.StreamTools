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
		) {

			var sp = services.BuildServiceProvider();
			var svc = new Services.TwitchService(Configuration, sp.GetService<ILoggerFactory>());
			services.AddSingleton<IHostedService>(svc);
			services.AddSingleton(svc);

			var mxr = new MixerService(Configuration, sp.GetService<ILoggerFactory>());
			services.AddSingleton<IHostedService>(mxr);
			services.AddSingleton(mxr);

			services.AddSingleton<StreamService>();

		}

		private static void ConfigureAspNetFeatures(IServiceCollection services)
		{
			services.AddSignalR();
			services.AddSingleton<FollowerHub>();

			services.AddMvc();
		}


	}
}
