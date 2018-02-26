using System;

namespace Fritz.StreamTools.Services
{
	public class ChatUserInfoEventArgs : EventArgs
	{
		public string ServiceName { get; set; }
		public int UserId { get; set; }
		public string UserName { get; set; }
	}
}
