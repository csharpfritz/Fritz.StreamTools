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
			if (eventName != "live")
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
			_logger.LogInformation("{0} {1}", payload.User.Username, payload.Following ? "followed" : "unfollowed");
		}

		void HandleHosted(WS.HostedPayload payload)
		{
			_logger.LogInformation("{0} started hosting for {1} viewers", payload.Hoster.Name, payload.Hoster.ViewersCurrent);
		}

		void HandleUnhosted(WS.HostedPayload payload)
		{
			_logger.LogInformation("{0} stopped hosting", payload.Hoster.Name);
		}

		void HandleSubscribed(WS.SubscribedPayload payload)
		{
			_logger.LogInformation("{0} subscribed", payload.User.Username);
		}

		void HandleResubscribed(WS.ResubSharedPayload payload)
		{
			_logger.LogInformation("{0} re-subscribed since {1} for {1} month", payload.User.Username, payload.Since, payload.TotalMonths);
		}
	}
}
