using System;
using System.Collections.Generic;
using System.Text;

namespace Fritz.Chatbot.Commands
{

	public class SentimentSink
	{

		public static Queue<string> RecentChatMessages { get; } = new Queue<string>();

	}

}
