using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Fritz.StreamTools.Services.Mixer
{
	public class ConstellationEventParser : IEventParser
	{
		public int Followers { get; set; }
		public int Viewers { get; set; }
		public bool? IsOnline { get; set; }
		public DateTime? StreamStartedAt { get; set; }

		private readonly ILogger _logger;
		private readonly Action<string, EventArgs> _fireEvent;

		public ConstellationEventParser(ILogger logger, Action<string, EventArgs> fireEvent)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_fireEvent = fireEvent ?? throw new ArgumentNullException(nameof(fireEvent));
		}

		public bool IsChat { get => false; }

		public void Process(string eventName, JToken data)
		{
			if (eventName != "live" || data == null)
				return;

			var channel = data["channel"]?.Value<string>().Split(':');
			if (channel == null || channel.Length == 0)
				return;
			var payload = data["payload"];
			if (payload == null || payload.Type != JTokenType.Object)
				return;

			if (channel[0] == "channel")
			{
				var channelId = uint.Parse(channel[1]);
				HandleChannelEvent(channel.Last(), channelId, payload);
			}
		}

		private void HandleChannelEvent(string eventName, uint channelId, JToken data)
		{
			switch (eventName)
			{
				case "update":
					HandleUpdate(data.GetObject<WS.LivePayload>());
					break;
				case "followed":
					HandleFollowed(data.GetObject<WS.FollowedPayload>());
					break;
				case "subscribed":
					HandleSubscribed(data.GetObject<WS.SubscribedPayload>());
					break;
				case "resubscribed":
				case "resubShared":
					HandleResubscribed(data.GetObject<WS.ResubSharedPayload>());
					break;
				case "hosted":
					HandleHosted(data.GetObject<WS.HostedPayload>());
					break;
				case "unhosted":
					HandleUnhosted(data.GetObject<WS.HostedPayload>());
					break;
			}
		}

		private void HandleUpdate(WS.LivePayload data)
		{
			ServiceUpdatedEventArgs update = null;

			if (data.NumFollowers.HasValue && data.NumFollowers != Followers)
			{
				Followers = (int)data.NumFollowers.Value;
				update = update ?? new ServiceUpdatedEventArgs();
				update.NewFollowers = Followers;
				_logger.LogTrace($"New followers count: {Followers}");
			}

			if (data.ViewersCurrent.HasValue)
			{
				var count = (int)data.ViewersCurrent.Value;
				if (count != Viewers)
				{
					Viewers = count;
					update = update ?? new ServiceUpdatedEventArgs();
					update.NewViewers = count;
					_logger.LogTrace($"New viewers count: {count}");
				}
			}

			if (data.Online.HasValue)
			{
				update = update ?? new ServiceUpdatedEventArgs();
				update.IsOnline = IsOnline = data.Online.Value;
				StreamStartedAt = null;  // Clear cached stream start time
				_logger.LogTrace($"Online status changed to: {update.IsOnline}");
			}

			if (update != null)
			{
				update.ServiceName = MixerService.SERVICE_NAME;
				_fireEvent(nameof(MixerService.Updated), update);
			}
		}

		void HandleFollowed(WS.FollowedPayload payload)
		{
			_fireEvent(nameof(MixerService.Followed),
				new FollowedEventArgs { IsFollowing = payload.Following, UserName = payload.User.Username });
		}

		void HandleHosted(WS.HostedPayload payload)
		{
			_fireEvent(nameof(MixerService.Hosted),
				new HostedEventArgs { IsHosting = true, HosterName = payload.Hoster.Name, CurrentViewers = payload.Hoster.ViewersCurrent });
		}

		void HandleUnhosted(WS.HostedPayload payload)
		{
			_fireEvent(nameof(MixerService.Hosted),
				new HostedEventArgs { IsHosting = false, HosterName = payload.Hoster.Name, CurrentViewers = payload.Hoster.ViewersCurrent });
		}

		void HandleSubscribed(WS.SubscribedPayload payload)
		{
			_fireEvent(nameof(MixerService.Subscribed), new SubscribedEventArgs { UserName = payload.User.Username });
		}

		void HandleResubscribed(WS.ResubSharedPayload payload)
		{
			_fireEvent(nameof(MixerService.Resubscribed),
				new ResubscribedEventArgs { UserName = payload.User.Username, Since = payload.Since, TotalMonths = payload.TotalMonths });
		}
	}
}
