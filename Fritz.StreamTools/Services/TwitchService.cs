using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Models.API.v5.Streams;
using TwitchLib.Services;

namespace Fritz.StreamTools.Services
{


  public class TwitchService : IHostedService
  {

    /// <summary>
    /// Service for connecting and monitoring Twitch
    /// </summary>
    public FollowerService Service { get; private set; }
    public IConfiguration Configuration { get; }

    public event EventHandler<ServiceUpdatedEventArgs> Updated;

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

    public static int _CurrentViewerCount;

    public int CurrentViewerCount {  get { return _CurrentViewerCount; } }

    private string ClientId { get { return Configuration["StreamServices:Twitch:ClientId"];  }  }

    private string Channel { get { return Configuration["StreamServices:Twitch:Channel"]; } }

    private string ChannelId {  get{ return Configuration["StreamServices:Twitch:UserId"]; } }


    private async Task StartTwitchMonitoring()
    {
      var api = new TwitchLib.TwitchAPI(clientId: ClientId);
      Service = new FollowerService(api);
      Service.SetChannelByName(Channel);
      await Service.StartService();

      var v5 = new TwitchLib.Channels.V5(api);
      var follows = await v5.GetAllFollowersAsync(ChannelId);

      _CurrentFollowerCount = follows.Count;
      Service.OnNewFollowersDetected += Service_OnNewFollowersDetected;
    }

    private void Service_OnNewFollowersDetected(object sender, 
    TwitchLib.Events.Services.FollowerService.OnNewFollowersDetectedArgs e)
    {
      Interlocked.Increment(ref _CurrentFollowerCount);
      Updated?.Invoke(this, new ServiceUpdatedEventArgs
      {
        ServiceName = "Twitch",
        NewFollowers = _CurrentFollowerCount
      });
    }

    private Task StopTwitchMonitoring()
    {
      Service.StopService();
      return Task.CompletedTask;
    }
  }

}
