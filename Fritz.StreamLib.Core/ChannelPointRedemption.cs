using System;
using System.Collections.Generic;
using System.Text;

namespace Fritz.StreamLib.Core
{
	public class ChannelPointRedemption
	{

		public string RedeemingUserName { get; set; }

		public string RedeemingUserId { get; set; }

		public string RewardName { get; set; }

		public int RewardValue { get; set; }

		public string RewardPrompt { get; set; }

		public string BackgroundColor { get; set; }

		public Uri Image_1x { get; set; }

		public Uri Image_2x { get; set; }

		public Uri Image_4x { get; set; }

	}
}
