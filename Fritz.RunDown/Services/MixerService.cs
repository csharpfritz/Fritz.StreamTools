using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.RunDown.Services
{
  public class MixerService : IHostedService
  {

    private IConfiguration Configuration;

    public MixerService(IConfiguration configuration)
    {
      this.Configuration = configuration;

    }

    private static int _CurrentFollowerCount = 0;
    private Timer _Timer;
    private HttpClient _Client;

    public int CurrentFollowerCount { get { return _CurrentFollowerCount; } }

    public string ChannelUrl { get
      {
        return $"https://mixer.com/api/v1/channels/{Configuration["StreamServices:Mixer:Channel"]}";
      }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      await ConnectToMixer();

    }
    public Task StopAsync(CancellationToken cancellationToken)
    {

      _Timer.Dispose();

      return Task.CompletedTask;
    }

    private async Task ConnectToMixer()
    {

       _Timer = new Timer(NewFollowerCheck, null, 0, 5000);

    }

    private void NewFollowerCheck(object state)
    {

      _Client = new HttpClient();
      var myTask = _Client.GetStringAsync(ChannelUrl);
      Task.WaitAll(myTask);
      var response = myTask.Result;
      var doc = JObject.Parse(response);
      var localCount = doc["numFollowers"].Value<int>();

      Interlocked.Exchange(ref _CurrentFollowerCount, localCount);

    }
  }

}
