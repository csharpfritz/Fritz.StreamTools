// REF: https://dev.mixer.com/reference/constellation/index.html

using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Fritz.StreamTools.Services
{
  public class FollowerClient
  {

    public FollowerClient(IHubContext<FollowerHub> followerContext)
    {

      this.FollowerContext = followerContext;

    }

    private IHubContext<FollowerHub> FollowerContext { get; }

    public void UpdateFollowers (int newFollowers) {

      FollowerContext.Clients.All.InvokeAsync("OnFollowersCountUpdated", newFollowers);

    }

  }

}
