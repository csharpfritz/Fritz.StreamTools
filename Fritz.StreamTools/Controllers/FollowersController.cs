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
                IHostingEnvironment env,
                FollowerClient followerClient,
                IConfiguration appConfig)
        {
            this.StreamService = streamService;
            this.Configuration = config.Value;


            this.HostingEnvironment = env;
            this.FollowerClient = followerClient;

            this.AppConfig = appConfig;
        }

        public StreamService StreamService { get; }
        public FollowerGoalConfiguration Configuration { get; }
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

        public IActionResult Count()
        {

            return View(StreamService.CurrentFollowerCount);

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

            model.LoadDefaultValues(Configuration);
            model.CurrentValue = Configuration.CurrentValue == -1 ? StreamService.CurrentFollowerCount : model.CurrentValue;


            return View(model);
        }


        [Route("followers/goal/configuration", Name = "ConfigureGoal")]
        public IActionResult GoalConfiguration()
        {

            ViewBag.GoogleFontsApiKey = AppConfig["GoogleFontsApi:Key"];
            return View();

        }
    }
}