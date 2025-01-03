using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Fritz.Twitch;
using Fritz.Twitch.PubSub;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
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
		private static string[] _OldManSoundFx;
		public static short _OldManCount = 0;
		private readonly Twitch.PubSub.Proxy _Proxy;
		private readonly ConfigurationSettings _Configuration;
		private readonly IWebHostEnvironment _HostEnvironment;
    private readonly ILogger _Logger;
    private readonly Dictionary<string, Action<IHubContext<AttentionHub, IAttentionHubClient>>> _ChannelPointActions = new Dictionary<string, Action<IHubContext<AttentionHub, IAttentionHubClient>>>();

		public TwitchPubSubService(IServiceProvider serviceProvider, Twitch.PubSub.Proxy proxy, IWebHostEnvironment hostEnvironment, IOptions<ConfigurationSettings> settings, ILoggerFactory loggerFactory)
		{
			_ServiceProvider = serviceProvider;
			_Proxy = proxy;
			_Configuration = settings.Value;
			_HostEnvironment = hostEnvironment;

			_Logger = loggerFactory.CreateLogger("TwitchPubSub");

			InitializeChannelPointActions();

		}

		private void InitializeChannelPointActions()
		{
			_ChannelPointActions.Add("get off my lawn!", (c) => TwitchPubSubService.OldManDeveloper(c));
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{

			if (string.IsNullOrEmpty(_Configuration.PubSubAuthToken)) {
				_Logger.LogError("Twitch PubSub token was not provided, unable to start this service");
				return Task.CompletedTask;
			}

			_Proxy.OnChannelPointsRedeemed += _Proxy_OnChannelPointsRedeemed;
			_Token = cancellationToken;
			_BackgroundTask = _Proxy.StartAsync(new TwitchTopic[] { TwitchTopic.ChannelPoints(_Configuration.UserId) }, _Token);
			IdentifyOldManAudio();
			return Task.CompletedTask;
		}

		private void IdentifyOldManAudio()
		{

			var oldManPath = Path.Combine(_HostEnvironment.WebRootPath, "contents", "oldman");
			var di = new DirectoryInfo(oldManPath);

			if (di.Exists && di.GetFiles().Any()) {
				_OldManSoundFx = di.GetFiles().Select(f => f.Name).OrderBy(x => Guid.NewGuid()).ToArray();
			}

		}

		private void _Proxy_OnChannelPointsRedeemed(object sender, ChannelRedemption e)
		{
			using (var scope = _ServiceProvider.CreateScope())
			{
				var context = scope.ServiceProvider.GetRequiredService<IHubContext<AttentionHub, IAttentionHubClient>>();

				if (_ChannelPointActions.ContainsKey(e.redemption.reward.title.ToLowerInvariant())) {
					_ChannelPointActions[e.redemption.reward.title.ToLowerInvariant()](context);
				}
				else
				{

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
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_Proxy.Dispose();
			return Task.CompletedTask;

		}

		#region Channel Point Actions

		public static void OldManDeveloper(IHubContext<AttentionHub, IAttentionHubClient> hubContext)
		{

			var fx = _OldManSoundFx[_OldManCount++ % _OldManSoundFx.Length];
			hubContext.Clients.All.PlaySoundEffect($"oldman/{fx}");

		}

		#endregion

	}
}
