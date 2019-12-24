using System;
using System.Collections.Generic;
using Fritz.StreamTools.Hubs;
using Fritz.StreamTools.Services;
using Fritz.Twitch;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fritz.StreamTools
{
  public class Startup
	{
		private static Dictionary<Type, string[]> _servicesRequiredConfiguration = new Dictionary<Type, string[]>()
		{
			{ typeof(SentimentService), new [] { "FritzBot:SentimentAnalysisKey" } }
		};

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddTwitchClient();

			StartupServices.ConfigureServices.Execute(services, Configuration, _servicesRequiredConfiguration);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostEnvironment env, IConfiguration config)
		{

			// Cheer 100 Crazy240sx 12/18/2018

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
			}

			app.UseHsts();
			app.UseHttpsRedirection();

			app.UseStaticFiles();

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{

				endpoints.MapHub<FollowerHub>("/followerstream");
				endpoints.MapHub<GithubyMcGithubFace>("/github");
				endpoints.MapHub<AttentionHub>("/attentionhub");

				endpoints.MapDefaultControllerRoute();

			});

		}
	}
}
