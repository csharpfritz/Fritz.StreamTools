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

		public decimal? AnswerPct { get; set; }

		[MaxLength(1000)]
		public string? AnswerTextProvided { get; set; }

		[Required]
		public DateTime ReviewDate { get; set; } = new DateTime(2079, 6, 1);

	}
}
