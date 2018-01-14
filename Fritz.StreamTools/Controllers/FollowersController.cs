using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamTools.Hubs;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
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
		  IHostingEnvironment env,
		  FollowerClient followerClient)
		{
			this.StreamService = streamService;
			this.Configuration = config.Value;
			this.HostingEnvironment = env;
			this.FollowerClient = followerClient;
		}

		public StreamService StreamService { get; }
		public FollowerGoalConfiguration Configuration { get; }
		public IHostingEnvironment HostingEnvironment { get; }
		public FollowerClient FollowerClient { get; }


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

		public IActionResult Count()
		{

			return View(StreamService.CurrentFollowerCount);

		}

		[Route("followers/goal/{*stuff}")]
		public IActionResult Goal(string stuff)
		{

			return View("Docs_Goal");

		}

		[Route("followers/goal/{goal:int}/{caption:maxlength(25)}")]
		public IActionResult Goal(string caption = "", int goal = 0, int width = 800, int current = -1)
		{


			// TODO: Handle empty caption

			/**
			 * Default value: 'Follower Goal'
			 * Lowest priority: whats in Configuration
			 * Highest priority: querystring
			 */

			caption = string.IsNullOrEmpty(caption) ? Configuration.Caption : caption == "null" ? "" : caption;
			goal = goal == 0 ? Configuration.Goal : goal;

			ViewBag.Width = width;
			ViewBag.Gradient = Gradient;

			return View(new FollowerGoal
			{
				Caption = caption,
				CurrentValue = current == -1 ? StreamService.CurrentFollowerCount : current,
				GoalValue = goal
			});

		}

		private string Gradient
		{
			get
			{
				var fillBackgroundColor = Configuration.FillBackgroundColor;
				var count = (double)fillBackgroundColor.Length;
				var percent = (double)1 / count;
				var colorWidth = (int)(ViewBag.Width * percent);
				const int blendWidth = 10;

				var result = new StringBuilder(fillBackgroundColor[0]);
				for (var c = 0; c < count - 1; c++)
				{
					var distance = (c + 1) * colorWidth;
					result.Append($", {fillBackgroundColor[c]} {distance - blendWidth }px, {fillBackgroundColor[c + 1]} {(c + 1) * colorWidth + blendWidth}px");
				}
				return result.ToString();
			}
		}

	}
}