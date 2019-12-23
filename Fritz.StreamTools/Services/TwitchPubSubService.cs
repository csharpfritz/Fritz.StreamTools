using Fritz.StreamTools.Hubs;
using Fritz.Twitch;
using Fritz.Twitch.PubSub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

		private IServiceProvider _ServiceProvider;
		private readonly Twitch.PubSub.Proxy _Proxy;
		private readonly ConfigurationSettings _Configuration;

		public TwitchPubSubService(IServiceProvider serviceProvider, Twitch.PubSub.Proxy proxy, IOptions<ConfigurationSettings> settings)
		{
			_ServiceProvider = serviceProvider;
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
			using (var scope = _ServiceProvider.CreateScope())
			{
				var context = scope.ServiceProvider.GetRequiredService<IHubContext<AttentionHub, IAttentionHubClient>>();
				context.Clients.All.PlaySoundEffect("pointsredeemed.mp3");
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_Proxy.Dispose();
			return Task.CompletedTask;
		}
	}
}
