using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Fritz.StreamTools.Pages
{
  public class CurrentViewersModel : PageModel
  {

    public CurrentViewersModel(
      TwitchService twitchService,
      MixerService mixerService
    )
    {
      this.Twitch = twitchService;
      this.Mixer = mixerService;
    }

    public TwitchService Twitch { get; }
    public MixerService Mixer { get; }

    public void OnGet()
    {

    }

  }
}