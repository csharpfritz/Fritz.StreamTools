using System;
using System.Collections.Generic;
using System.Linq;
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
      TwitchService twitch, 
      MixerService mixer, 
      IOptions<FollowerGoalConfiguration> config,
      IHostingEnvironment env,
      FollowerClient followerClient) 
    {
      this.TwitchService = twitch;
      this.MixerService = mixer;
      this.Configuration = config.Value;
      this.HostingEnvironment = env;
      this.FollowerClient = followerClient;
    }

    public TwitchService TwitchService { get; }
    public MixerService MixerService { get; }
    public FollowerGoalConfiguration Configuration { get; }
    public IHostingEnvironment HostingEnvironment { get; }
    public FollowerClient FollowerClient { get; }


    [HttpGet("api/Followers")]
    public int Get()
    {

      if (HostingEnvironment.IsDevelopment() && _TestFollowers > 0) {
        return _TestFollowers;
      }

      return TwitchService.CurrentFollowerCount + MixerService.CurrentFollowerCount;
    }

    [HttpPost("api/Followers")] 
    public void Post(int newFollowers) {

      if (HostingEnvironment.IsDevelopment()) {
        _TestFollowers = newFollowers;
        FollowerClient.UpdateFollowers(newFollowers);
      }

    }

    public IActionResult Count() {

      return View(TwitchService.CurrentFollowerCount + MixerService.CurrentFollowerCount);

    }

    [Route("followers/goal/{*stuff}")]
    public IActionResult Goal(string stuff) {

      return View("Docs_Goal");

    }

    [Route("followers/goal/{goal:int}/{caption:maxlength(25)}")]
    public IActionResult Goal(string caption = "", int goal=0, int width = 800) {


      // TODO: Handle empty caption

      /**
       * Default value: 'Follower Goal'
       * Lowest priority: whats in Configuration
       * Highest priority: querystring
       */

      caption = string.IsNullOrEmpty(caption) ? Configuration.Caption : caption == "null" ? "" : caption;
      goal = goal == 0 ? Configuration.Goal : goal;

      ViewBag.Width = width;

      return View(new FollowerGoal
      {
        Caption = caption,
        CurrentValue = TwitchService.CurrentFollowerCount + MixerService.CurrentFollowerCount,
        GoalValue = goal
      });

    }

  }
}