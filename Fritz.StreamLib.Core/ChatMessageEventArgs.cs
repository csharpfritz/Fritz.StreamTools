using System;
using System.Collections.Generic;

namespace Fritz.StreamLib.Core
{
	public class ChatMessageEventArgs : EventArgs
	{
		/// <summary>Name of the originating service</summary>
		public string ServiceName { get; set; }

		/// <summary>Service specific properties (like AvatarUrl)</summary>
		public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

		public uint ChannelId { get; set; }
		public uint UserId { get; set; }
		public string UserName { get; set; }
		public bool IsWhisper { get; set; }
		public bool IsOwner { get; set; }
		public bool IsModerator { get; set; }
		public bool IsVip { get; set; }
		public string Message { get; set; }
	}
}
