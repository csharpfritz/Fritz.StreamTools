using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Hubs
{

  public class FollowerHub : Hub
  {
    public TwitchService Twitch { get; }
    public MixerService Mixer { get; }

    public FollowerHub(
      TwitchService twitch,
      MixerService mixer
      ) {

      this.Twitch = twitch;
      this.Mixer = mixer;

      Mixer.Updated += StreamService_Updated;
      Twitch.Updated += StreamService_Updated;
    }

    private void StreamService_Updated(object sender, EventArgs e) {
      Clients.All.InvokeAsync("OnFollowersCountUpdated", this.Mixer.CurrentFollowerCount + this.Twitch.CurrentFollowerCount);
    }
  }

}
