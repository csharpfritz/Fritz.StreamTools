using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Models.API.v5.Streams;
using TwitchLib.Services;

namespace Fritz.StreamTools.Services
{


  public class TwitchService : IHostedService, IStreamService {

    /// <summary>
    /// Service for connecting and monitoring Twitch
    /// </summary>
    public FollowerService Service { get; private set; }
    private IConfiguration Configuration { get; }
    public ILogger Logger { get; }

    public event EventHandler<ServiceUpdatedEventArgs> Updated;

    public TwitchService(IConfiguration config, ILoggerFactory loggerFactory)
    {
      this.Configuration = config;
      this.Logger = loggerFactory.CreateLogger("StreamServices");
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

      var v5Stream = new TwitchLib.Streams.V5(api);
      var myStream = await v5Stream.GetStreamByUserAsync(ChannelId);
      _CurrentViewerCount = myStream.Stream?.Viewers ?? 0;

      Logger.LogInformation($"Now monitoring Twitch with {_CurrentFollowerCount} followers and {_CurrentViewerCount} Viewers");

      _Timer = new Timer(CheckViews, null, 0, 5000);

    }

    private async void CheckViews(object state)
    {

      var api = new TwitchLib.TwitchAPI(clientId: ClientId);
      var v5Stream = new TwitchLib.Streams.V5(api);
			StreamByUser myStream = null;

			try
			{

				myStream = await v5Stream.GetStreamByUserAsync(ChannelId);

			} catch (JsonReaderException ex) {

				Logger.LogError($"Unable to read stream from Twitch: {ex}");
				return;

			}

      if (_CurrentViewerCount != (myStream.Stream?.Viewers ?? 0))
      {
        _CurrentViewerCount = (myStream.Stream?.Viewers ?? 0);
        Updated?.Invoke(null, new ServiceUpdatedEventArgs
        {
          ServiceName = "Twitch",
          NewViewers = _CurrentViewerCount
        });
      }

    }

    private void Service_OnNewFollowersDetected(object sender, 
    TwitchLib.Events.Services.FollowerService.OnNewFollowersDetectedArgs e)
    {
      Interlocked.Exchange(ref _CurrentFollowerCount, _CurrentFollowerCount + e.NewFollowers.Count);
      Logger.LogInformation($"New Followers on Twitch, new total: {_CurrentFollowerCount}");

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
