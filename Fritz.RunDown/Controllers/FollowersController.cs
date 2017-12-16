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

    public FollowersController(TwitchService twitch, MixerService mixer)
    {
      this.TwitchService = twitch;
      this.MixerService = mixer;
    }

    public TwitchService TwitchService { get; }
    public MixerService MixerService { get; }

    public int Index()
    {
      return TwitchService.CurrentFollowerCount + MixerService.CurrentFollowerCount;
    }
  }
}