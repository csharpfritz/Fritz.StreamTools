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
        // public IActionResult Goal(string caption = "", int goal = 0,
        //     int width = 800, int current = -1, string bgcolors = "",
        //     string bgblend = "", string emptyBgColor = "", string emptyFontColor = "",
        //     string fontName = "~")
        public IActionResult Goal(FollowerGoalConfiguration model)
        {
            if (!ModelState.IsValid)
            {
                // TODO: Route to Docs View
                // Set error message in ViewBag
                return View("Docs_Goal");
            }

            // load unspecified values from configuration

            model.Caption = string.IsNullOrEmpty(model.Caption) ? Configuration.Caption : model.Caption == "null" ? "" : model.Caption;
            model.Goal = model.Goal == 0 ? Configuration.Goal : model.Goal;
            var backColors = string.IsNullOrEmpty(model.FillBackgroundColor) ? Configuration.FillBgColorArray : model.FillBackgroundColor.Split(',');
            var backBlend = string.IsNullOrEmpty(model.FillBackgroundColorBlend) ? Configuration.FillBgBlendArray : model.FillBackgroundColorBlend.Split(',').Select(a => double.Parse(a)).ToArray();

            model.EmptyBackgroundColor = string.IsNullOrWhiteSpace(model.EmptyBackgroundColor) ? Configuration.EmptyBackgroundColor : model.EmptyBackgroundColor;
            model.EmptyFontColor = string.IsNullOrWhiteSpace(model.EmptyFontColor) ? Configuration.EmptyFontColor : model.EmptyFontColor;
            model.FontName = model.FontName == "~" ? Configuration.FontName : model.FontName;
            model.CurrentValue = model.CurrentValue == -1 ? StreamService.CurrentFollowerCount : model.CurrentValue;
            //Configuration.Goal = goal;
            //Configuration.Caption = caption;
            //Configuration.Width = width;
            model.Gradient = DisplayHelper.Gradient(backColors, backBlend, model.Width);

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