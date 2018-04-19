using System;

namespace Fritz.Twitch
{
	public class NewFollowersEventArgs : EventArgs
	{
		private int foundFollowerCount;

		public NewFollowersEventArgs(int foundFollowerCount)
		{
			this.foundFollowerCount = foundFollowerCount;
		}

		public int FollowerCount => foundFollowerCount;

	}
}
