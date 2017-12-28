using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Fritz.StreamTools.Controllers
{

  public class FollowersController : Controller
  {

    public FollowersController(TwitchService twitch, MixerService mixer, IOptions<FollowerGoalConfiguration> config)
    {
      this.TwitchService = twitch;
      this.MixerService = mixer;
      this.Configuration = config.Value;
    }

    public TwitchService TwitchService { get; }
    public MixerService MixerService { get; }
    public FollowerGoalConfiguration Configuration { get; }


    [HttpGet("api/Followers")]
    public int Get()
    {
      return TwitchService.CurrentFollowerCount + MixerService.CurrentFollowerCount;
    }

    public IActionResult Count() {

      return View(TwitchService.CurrentFollowerCount + MixerService.CurrentFollowerCount);

    }

    [Route("followers/goal/{goal=0}/{caption=}")]
    public IActionResult Goal(string caption, int goal, int width = 800) {


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