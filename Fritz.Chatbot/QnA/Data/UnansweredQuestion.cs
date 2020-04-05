using System;
using System.ComponentModel.DataAnnotations;

namespace Fritz.Chatbot.QnA.Data
{
	public class UnansweredQuestion
	{

		public int Id { get; set; }

		[Required]
		public DateTime AskedDateStamp { get; set; } = DateTime.UtcNow;

		[Required]
		[MaxLength(1000)]
		public string QuestionText { get; set; }

	}
}
