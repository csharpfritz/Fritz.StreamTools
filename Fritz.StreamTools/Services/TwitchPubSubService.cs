using Fritz.StreamTools.Hubs;
using Fritz.Twitch;
using Fritz.Twitch.PubSub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Services
{
	public class TwitchPubSubService : IHostedService
	{

		private readonly IHubContext<AttentionHub, IAttentionHubClient> _HubContext;
		private readonly Twitch.PubSub.Proxy _Proxy;
		private readonly ConfigurationSettings _Configuration;

		public TwitchPubSubService(IHubContext<AttentionHub, IAttentionHubClient> hubContext, Twitch.PubSub.Proxy proxy, IOptions<ConfigurationSettings> settings)
		{
			_HubContext = hubContext;
			_Proxy = proxy;
			_Configuration = settings.Value;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_Proxy.OnChannelPointsRedeemed += _Proxy_OnChannelPointsRedeemed;
			return _Proxy.StartAsync(new TwitchTopic[] { TwitchTopic.ChannelPoints(_Configuration.UserId) }, cancellationToken);
		}

		private void _Proxy_OnChannelPointsRedeemed(object sender, ChannelRedemption e)
		{
			_HubContext.Clients.All.PlaySoundEffect("pointsredeemed.mp3");
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_Proxy.Dispose();
			return Task.CompletedTask;
		}
	}
}
