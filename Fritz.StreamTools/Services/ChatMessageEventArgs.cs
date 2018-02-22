using System;

namespace Fritz.StreamTools.Services
{
	public class ChatMessageEventArgs : EventArgs
	{
		public string ServiceName { get; set; }
		public int UserId { get; set; }
		public string UserName { get; set; }
		public bool IsOwner { get; set; }
		public bool IsModerator { get; set; }
		public string Message { get; set; }
	}
}
