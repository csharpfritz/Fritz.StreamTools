using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class SentimentCommand : IBasicCommand
	{
		public string Trigger => "sentiment";
		public string Description => "Information about the sentiment analysis features of the chat room";
		public TimeSpan? Cooldown => TimeSpan.FromSeconds(30);

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			var sb = new StringBuilder();
			sb.Append($"The sentiment over the last minute is {SentimentSink.Sentiment1Minute.ToString("00.0%")} percent positive ");
			sb.Append((SentimentSink.Sentiment1Minute < SentimentSink.Sentiment5Minute) ? "and trending upward (up arrow) " : "and trending downward (down arrow) ");
			sb.Append("compared to the last five minutes ");
			sb.Append($"The most recent messages were {((SentimentSink.SentimentInstant >=0.5) ? "positive" : "negative")} (smiley face)");

			await chatService.SendMessageAsync(sb.ToString());

		}
	}
}
