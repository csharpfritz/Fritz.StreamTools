using System;
using System.Collections.Generic;
using System.Text;

namespace Fritz.Chatbot.Commands
{

	public class SentimentSink
	{

		public static Queue<string> RecentChatMessages { get; } = new Queue<string>();

		public static double SentimentInstant { get; set; }

		public static double Sentiment1Minute { get; set; }

		public static double Sentiment5Minute { get; set; }

	}

}
