using System;

namespace Fritz.Twitch
{
	public class NewViewersEventArgs : EventArgs
	{
		private int foundViewerCount;

		public NewViewersEventArgs(int foundViewerCount)
		{
			this.foundViewerCount = foundViewerCount;
		}

		public int ViewerCount => foundViewerCount;

	}
}
