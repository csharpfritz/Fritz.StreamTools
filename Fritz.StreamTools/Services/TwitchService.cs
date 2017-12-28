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
    private Timer _Timer;
    private StreamByUser _Stream;

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
      var viewers = 

      _CurrentFollowerCount = follows.Count;
      Service.OnNewFollowersDetected += Service_OnNewFollowersDetected;

      _Stream = await api.Streams.v5.GetStreamByUserAsync(ChannelId);
      _Timer = new Timer(UpdateViewers, null, 0, 5000);

    }

    private void UpdateViewers(object state)
    {
      
      var count = _Stream.Stream.Viewers;
      Interlocked.Exchange(ref _CurrentViewerCount, count);


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
