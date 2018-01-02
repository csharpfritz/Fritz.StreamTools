

using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace Fritz.StreamTools.Services
{
  public class MyFollowerService   {

    public MyFollowerService(
      TwitchService twitch, 
      MixerService mixer, 
      IConfiguration config, 
      IHubContext<FollowerHub> followerHubContext)
    {

      this.Twitch = twitch;
      this.Mixer = mixer;
      this.Config = config;
      this.FollowerContext = followerHubContext;

      Mixer.Updated += StreamService_Updated;
      Twitch.Updated += StreamService_Updated;

    }

    private void StreamService_Updated(object sender, System.EventArgs e)
    {
      FollowersUpdated();
    }

    public TwitchService Twitch { get; }
    public MixerService Mixer { get; }
    public IConfiguration Config { get; }
    public IHubContext<FollowerHub> FollowerContext { get; }

    private int TotalFollowerCount {  get {
        return Twitch.CurrentFollowerCount + Mixer.CurrentFollowerCount;
    } }

    internal void FollowersUpdated() {

      FollowerContext.Clients.All.InvokeAsync("NewFollowerCount", TotalFollowerCount);

    }


  }

}
