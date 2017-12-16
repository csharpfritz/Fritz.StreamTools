using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Services;

namespace Fritz.RunDown.Services
{


  public class TwitchService : IHostedService
  {

    /// <summary>
    /// Service for connecting and monitoring Twitch
    /// </summary>
    public FollowerService Service { get; private set; }
    public IConfiguration Configuration { get; }

    public TwitchService(IConfiguration config)
    {
      this.Configuration = config;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      return StartTwitchMonitoring();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      return StopTwitchMonitoring();
    }

    public static int _CurrentFollowerCount;
    public int CurrentFollowerCount { get { return _CurrentFollowerCount; } }


    private async Task StartTwitchMonitoring()
    {
      var api = new TwitchLib.TwitchAPI(clientId: Configuration["StreamServices:Twitch:ClientId"]);
      Service = new FollowerService(api);
      Service.SetChannelByName(Configuration["StreamServices:Twitch:Channel"]);
      await Service.StartService();

      var v5 = new TwitchLib.Channels.V5(api);
      var follows = await v5.GetAllFollowersAsync(Configuration["StreamServices:Twitch:UserId"]);

      _CurrentFollowerCount = follows.Count;
      Service.OnNewFollowersDetected += Service_OnNewFollowersDetected;

    }

    private void Service_OnNewFollowersDetected(object sender, 
    TwitchLib.Events.Services.FollowerService.OnNewFollowersDetectedArgs e)
    {

      Interlocked.Increment(ref _CurrentFollowerCount);

    }

    private Task StopTwitchMonitoring()
    {
      Service.StopService();
      return Task.CompletedTask;
    }
  }

}
