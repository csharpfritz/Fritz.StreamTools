using Fritz.StreamLib.Core;
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
		private CancellationToken _Token;
		private Task _BackgroundTask;
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
			_Token = cancellationToken;
			_BackgroundTask = _Proxy.StartAsync(new TwitchTopic[] { TwitchTopic.ChannelPoints(_Configuration.UserId) }, _Token);
			return Task.CompletedTask;
		}

		private void _Proxy_OnChannelPointsRedeemed(object sender, ChannelRedemption e)
		{
			using (var scope = _ServiceProvider.CreateScope())
			{
				var context = scope.ServiceProvider.GetRequiredService<IHubContext<AttentionHub, IAttentionHubClient>>();
				//context.Clients.All.PlaySoundEffect("pointsredeemed.mp3");
				var redemption = new ChannelPointRedemption
				{
					RedeemingUserName = e.redemption.user.display_name,
					RedeemingUserId = e.redemption.user.id,
					RewardName = e.redemption.reward.title,
					RewardValue = e.redemption.reward.cost,
					RewardPrompt = e.redemption.user_input,
					BackgroundColor = e.redemption.reward.background_color,
					Image_1x = new Uri(e.redemption.reward.image?.url_1x ?? e.redemption.reward.default_image.url_1x),
					Image_2x = new Uri(e.redemption.reward.image?.url_2x ?? e.redemption.reward.default_image.url_2x),
					Image_4x = new Uri(e.redemption.reward.image?.url_4x ?? e.redemption.reward.default_image.url_4x)
				};
				context.Clients.All.NotifyChannelPoints(redemption);
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_Proxy.Dispose();
			return Task.CompletedTask;
		}
	}
}
