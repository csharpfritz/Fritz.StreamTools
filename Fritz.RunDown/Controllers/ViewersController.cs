using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.RunDown.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fritz.RunDown.Controllers
{
  public class ViewersController : Controller
  {

    public ViewersController(TwitchService twitch, MixerService mixer)
    {
      this.Twitch = twitch;
      this.Mixer = mixer;
    }

    public TwitchService Twitch { get; }
    public MixerService Mixer { get; }

    public IActionResult Current()
    {
      return View(Twitch.CurrentViewerCount + Mixer.CurrentViewerCount);
    }
  }
}