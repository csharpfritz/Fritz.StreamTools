using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.RunDown.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fritz.RunDown.Controllers
{
  public class FollowersController : Controller
  {

    public FollowersController(TwitchService twitch)
    {
      this.TwitchService = twitch;
    }

    public TwitchService TwitchService { get; }

    public int Index()
    {
      return TwitchService.CurrentFollowerCount;
    }
  }
}