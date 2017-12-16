using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Mixer.Base;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.RunDown.Services
{
  public class MixerService : IHostedService
  {

    private IConfiguration Configuration;

    private static readonly List<OAuthClientScopeEnum> _scopes = new List<OAuthClientScopeEnum>() {
        OAuthClientScopeEnum.channel__details__self,
        OAuthClientScopeEnum.channel__update__self
    };

    public MixerService(IConfiguration configuration)
    {
      this.Configuration = configuration;

    }

    private static int _CurrentFollowerCount;
    public int CurrentFollowerCount { get { return _CurrentFollowerCount; } }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      await ConnectToMixer();

    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
      // throw new NotImplementedException();
      return Task.CompletedTask;
    }

    private async Task ConnectToMixer()
    {
      var connection = await MixerConnection.ConnectViaLocalhostOAuthBrowser(
        Configuration["StreamServices:Mixer:ClientId"], _scopes);
      var myChannel = await connection.Channels.GetChannel("csharpfritz");
      _CurrentFollowerCount = (int)myChannel.numFollowers;
    }

  }

}
