using System;
using System.Collections.Generic;

namespace Test.Services.Mixer
{
	public static class Packets
	{
		public class MsgReplyMessage
		{
			public string type { get; set; }
			public string data { get; set; }
			public string text { get; set; }
		}

		public class Meta
		{
		}

		public class MetaWhisper : Meta
		{
			public bool whisper { get; set; }
		}

		public class MsgReplyMessages
		{
			public IList<MsgReplyMessage> message { get; set; }
			public Meta meta { get; set; }
		}

		public class MsgReplyData
		{
			public int channel { get; set; }
			public Guid id { get; set; }
			public string user_name { get; set; }
			public int user_id { get; set; }
			public int user_level { get; set; }
			public string user_avatar { get; set; }
			public IList<string> user_roles { get; set; }
			public MsgReplyMessages message { get; set; }
		}

		public class MsgReplyDataWhisper : MsgReplyData
		{
			public string target { get; set; }
		}

		public class MsgReply
		{
			public string type { get; set; }
			public object error { get; set; }
			public int id { get; set; }
			public MsgReplyData data { get; set; }
		}

		/*
		 * ChatMessage
		*/

		public class ChatMsgMessage
		{
			public string type { get; set; }
		}

		public class ChatMsgMessageText : ChatMsgMessage
		{
			public string data { get; set; }
			public string text { get; set; }
		}

		public class ChatMsgMessageLink : ChatMsgMessage
		{
			public string text { get; set; }
			public string url { get; set; }
		}

		public class ChatMsgMessages
		{
			public IList<ChatMsgMessage> message { get; set; }
		}

		public class ChatMsgMessagesMeta : ChatMsgMessages
		{
			public Meta meta { get; set; }
		}

		public class ChatMsgData
		{
			public int channel { get; set; }
			public Guid id { get; set; }
			public string user_name { get; set; }
			public int user_id { get; set; }
			public IList<string> user_roles { get; set; }
			public int user_level { get; set; }
			public string user_avatar { get; set; }
			public ChatMsgMessages message { get; set; }
		}

		public class ChatMsg
		{
			public string type { get; set; }
			public string @event{ get; set; }
			public ChatMsgData data { get; set; }
		}


	}
}
