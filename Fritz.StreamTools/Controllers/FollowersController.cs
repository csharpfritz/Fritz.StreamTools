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

      if (HostingEnvironment.IsDevelopment() && _TestFollowers > 0) {
        return _TestFollowers;
      }

      return StreamService.CurrentFollowerCount;
    }

    [HttpPost("api/Followers")] 
    public void Post(int newFollowers) {

      if (HostingEnvironment.IsDevelopment()) {
        _TestFollowers = newFollowers;
        FollowerClient.UpdateFollowers(newFollowers);
      }

    }

    public IActionResult Count() {

      return View(StreamService.CurrentFollowerCount);

    }

    [Route("followers/goal/{*stuff}")]
    public IActionResult Goal(string stuff) {

      return View("Docs_Goal");

    }

    [Route("followers/goal/{goal:int}/{caption:maxlength(25)}")]
    public IActionResult Goal(string caption = "", int goal=0, int width = 800, int current = -1, string bgcolors = "", string bgblend = "") {


      // TODO: Handle empty caption

      /**
       * Default value: 'Follower Goal'
       * Lowest priority: whats in Configuration
       * Highest priority: querystring
       */

      caption = string.IsNullOrEmpty(caption) ? Configuration.Caption : caption == "null" ? "" : caption;
      goal = goal == 0 ? Configuration.Goal : goal;
      var backColors = string.IsNullOrEmpty(bgcolors) ? Configuration.FillBackgroundColor : bgcolors.Split(',');
      var backBlend = string.IsNullOrEmpty(bgblend) ? Configuration.FillBackgroundColorBlend : bgblend.Split(',').Select(a => double.Parse(a)).ToArray();

      ViewBag.Width = width;
      ViewBag.Gradient = Gradient(backColors, backBlend, width);

      return View(new FollowerGoal
      {
        Caption = caption,
        CurrentValue = current == -1 ? StreamService.CurrentFollowerCount : current,
        GoalValue = goal
      });
    }

    /// <summary>
    /// Produces the color gradient string required for linear-gradient based on a set of colors and the requested blending
    /// </summary>
    /// <param name="bgcolors">An array of valid CSS colors such as red,green,blue or #F00,#0F0,#00F</param>
    /// <param name="bgblend">An array of percentage blends required for each color - expressed between 0 and 1 </param>
    /// <param name="width">The total width to blend over</param>
    /// <returns></returns>
    private string Gradient(string[] bgcolors, double[] bgblend, int width)
    {
      var count = (double)bgcolors.Length;
      var percent = (double)1 / count;
      var colorWidth = (int)(width * percent);
      
      var result = new StringBuilder(bgcolors[0]);
      for (var c = 0; c < count - 1; c++)
      {
        var distance = (c + 1) * colorWidth;
        var blendWidthLeft = 0;
        var blendWidthRight = 0;
      
        if (bgblend != null && bgblend.Length > c)
        {
          blendWidthLeft = (int)(colorWidth * bgblend[c]);
        }
        if (bgblend != null && bgblend.Length > c + 1)
        {
          blendWidthRight = (int)(colorWidth * bgblend[c + 1]);
        }
        result.Append($", {bgcolors[c]} {distance - blendWidthLeft }px, {bgcolors[c + 1]} {(c + 1) * colorWidth + blendWidthRight}px");
      }
      result.Append($", {bgcolors.Last()}");
      return result.ToString();
    }
  }
}