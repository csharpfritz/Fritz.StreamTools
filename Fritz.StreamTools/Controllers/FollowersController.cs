using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamTools.Helpers;
using Fritz.StreamTools.Hubs;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Fritz.StreamTools.Controllers
{

	public class FollowersController : Controller
	{
		internal static int _TestFollowers;

		public FollowersController(
						StreamService streamService,
						IOptions<FollowerGoalConfiguration> config,
						IOptions<FollowerCountConfiguration> countConfig,
						IHostingEnvironment env,
						FollowerClient followerClient,
						IConfiguration appConfig)
		{
			this.StreamService = streamService;
			this.GoalConfiguration = config.Value;
			this.CountConfiguration = countConfig.Value;


			this.HostingEnvironment = env;
			this.FollowerClient = followerClient;

			this.AppConfig = appConfig;
		}

		public StreamService StreamService { get; }
		public FollowerGoalConfiguration GoalConfiguration { get; }
		public FollowerCountConfiguration CountConfiguration { get; }
		public IHostingEnvironment HostingEnvironment { get; }
		public FollowerClient FollowerClient { get; }
		public IConfiguration AppConfig { get; }


		[HttpGet("api/Followers")]
		public int Get()
		{

			if (HostingEnvironment.IsDevelopment() && _TestFollowers > 0)
			{
				return _TestFollowers;
			}

			return StreamService.CurrentFollowerCount;
		}

		[HttpPost("api/Followers")]
		public void Post(int newFollowers)
		{

			if (HostingEnvironment.IsDevelopment())
			{
				_TestFollowers = newFollowers;
				FollowerClient.UpdateFollowers(newFollowers);
			}

		}

		public IActionResult Count(FollowerCountConfiguration model)
		{

			if (!ModelState.IsValid)
			{

				return View("Docs_Count");
			}

			// TODO: Read this from AppSettings?
			model.LoadDefaultSettings(CountConfiguration);

				if (model.CurrentValue == 0)
				{
					model.CurrentValue = StreamService.CurrentFollowerCount;
				}




			return View(model);

		}

		[Route("followers/count/configuration", Name ="ConfigurationFollowerCount")]
		public IActionResult CountConfigurationAction()
		{
			return View();
		}

		[Route("followers/goal/{*stuff}")]
		public IActionResult Goal(string stuff)
		{
			return View("Docs_Goal");
		}

		[ResponseCache(NoStore = true)]
		[Route("followers/goal/{goal:int}/{caption:maxlength(25)}")]
		public IActionResult Goal(FollowerGoalConfiguration model)
		{
			if (!ModelState.IsValid)
			{
				// TODO: Route to Docs View
				// Set error message in ViewBag
				return View("Docs_Goal");
			}

			model.LoadDefaultValues(GoalConfiguration);
			model.CurrentValue = model.CurrentValue <= 0 ? StreamService.CurrentFollowerCount : model.CurrentValue;


			return View(model);
		}


		[Route("followers/goal/configuration", Name = "ConfigureGoal")]
		public IActionResult GoalConfigurationAction()
		{

			ViewBag.GoogleFontsApiKey = AppConfig["GoogleFontsApi:Key"];
			return View();

		}
	}
}
