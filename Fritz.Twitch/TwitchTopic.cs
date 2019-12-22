using System.Collections.Generic;

namespace Fritz.Twitch
{

	public sealed class TwitchTopic
	{

		private TwitchTopic() { }

		public string TopicString { get; set; }


		public static TwitchTopic ChannelPoints(string channelId) {

		return new TwitchTopic
			{
				TopicString = $"channel-points-channel-v1.{channelId}"
			};

		}

	}

}
