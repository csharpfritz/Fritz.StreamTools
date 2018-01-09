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
    public FollowerClient FollowerClient { get; }

    public FollowerHub(
      TwitchService twitch,
      MixerService mixer,
      FollowerClient client
      ) {

      this.Twitch = twitch;
      this.Mixer = mixer;
      this.FollowerClient = client;

      Mixer.Updated += StreamService_Updated;
      Twitch.Updated += StreamService_Updated;
    }

    private void StreamService_Updated(object sender, ServiceUpdatedEventArgs e) {


      if (e.NewFollowers.HasValue)
      {
        this.FollowerClient.UpdateFollowers(this.Mixer.CurrentFollowerCount + this.Twitch.CurrentFollowerCount);
      }

      if (e.NewViewers.HasValue) {
        this.FollowerClient.UpdateViewers(e.ServiceName, e.NewViewers.Value);
      }


    }
  }

}
