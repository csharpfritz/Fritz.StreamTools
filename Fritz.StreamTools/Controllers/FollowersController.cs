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
        public IActionResult Goal(string caption = "", int goal = 0,
            int width = 800, int current = -1, string bgcolors = "",
            string bgblend = "", string emptyBgColor = "", string emptyFontColor = "",
            string fontName = "~")
        {


            // TODO: Handle empty caption

            /**
			 * Default value: 'Follower Goal'
			 * Lowest priority: whats in Configuration
			 * Highest priority: querystring
			 */

            caption = string.IsNullOrEmpty(caption) ? Configuration.Caption : caption == "null" ? "" : caption;
            goal = goal == 0 ? Configuration.Goal : goal;
            var backColors = string.IsNullOrEmpty(bgcolors) ? Configuration.FillBgColorArray : bgcolors.Split(',');
            var backBlend = string.IsNullOrEmpty(bgblend) ? Configuration.FillBgBlendArray : bgblend.Split(',').Select(a => double.Parse(a)).ToArray();

            Configuration.EmptyBackgroundColor = string.IsNullOrWhiteSpace(emptyBgColor) ? Configuration.EmptyBackgroundColor : emptyBgColor;
            Configuration.EmptyFontColor = string.IsNullOrWhiteSpace(emptyFontColor) ? Configuration.EmptyFontColor : emptyFontColor;
            Configuration.FontName = fontName == "~" ? Configuration.FontName : fontName;
            Configuration.CurrentValue = current == -1 ? StreamService.CurrentFollowerCount : current;
            Configuration.Goal = goal;
            Configuration.Caption = caption;
            Configuration.Width = width;
            Configuration.Gradient = DisplayHelper.Gradient(backColors, backBlend, width);

            return View(Configuration);
        }


        [Route("followers/goal/configuration", Name = "ConfigureGoal")]
        public IActionResult GoalConfiguration()
        {

            ViewBag.GoogleFontsApiKey = AppConfig["GoogleFontsApi:Key"];
            return View();

        }
    }
}