using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.StreamTools.Hubs;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fritz.StreamTools
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{

			services.AddSingleton<Models.RundownRepository>();

      services.Configure<FollowerGoalConfiguration>(Configuration.GetSection("FollowerGoal"));

      var svc = new Services.TwitchService(Configuration);
      services.AddSingleton<IHostedService>(svc);
      services.AddSingleton(svc);

      var mxr = new MixerService(Configuration);
      services.AddSingleton<IHostedService>(mxr);
      services.AddSingleton(mxr);

      services.AddSingleton<StreamService>();

      //services.AddScoped<MyFollowerService>();

      services.AddSignalR();
      services.AddSingleton<FollowerHub>();

      services.AddSingleton<FollowerClient>();


			services.AddMvc();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseBrowserLink();
			}
			else
			{
				app.UseExceptionHandler("/Error");
			}

			app.UseStaticFiles();

      app.UseSignalR(configure =>
      {
        configure.MapHub<FollowerHub>("followerstream");
      });

			app.UseMvc(routes =>
			{
				routes.MapRoute(
									name: "default",
									template: "{controller}/{action=Index}/{id?}");
			});
		}
	}
}
