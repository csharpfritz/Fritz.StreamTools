using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.RunDown.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fritz.RunDown
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
      
      var svc = new Services.TwitchService(Configuration);
      services.AddSingleton<IHostedService>(svc);
      services.AddSingleton(svc);

      var mxr = new MixerService(Configuration);
      services.AddSingleton<IHostedService>(mxr);
      services.AddSingleton(mxr);


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

			app.UseMvc(routes =>
			{
				routes.MapRoute(
									name: "default",
									template: "{controller}/{action=Index}/{id?}");
			});
		}
	}
}
