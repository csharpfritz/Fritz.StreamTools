using System;

namespace Fritz.Chatbot.QnA.QnAMaker
{
	public class Response
	{
		public string operationState { get; set; }
		public DateTime createdTimestamp { get; set; }
		public DateTime lastActionTimestamp { get; set; }
		public string userId { get; set; }
		public string operationId { get; set; }
	}


}
