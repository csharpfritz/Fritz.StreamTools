using System;

namespace Fritz.Twitch
{
	public class NewMessageEventArgs : EventArgs
	{

		public string UserName { get; set; }

		public string Message { get; set; }

		public bool IsWhisper { get; set; } = false;

	}
}
