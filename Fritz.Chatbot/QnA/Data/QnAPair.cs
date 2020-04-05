using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Fritz.Chatbot.QnA.Data
{

	public class QnAPair
	{

		[Key]
		public int Id { get; set; }

		[Required]
		[MaxLength(280)]		// Same length as a Tweet
		public string QuestionText { get; set; }

		[Required]
		[MaxLength(1000)]
		public string AnswerText { get; set; }

		public ICollection<AlternateQuestion> AlternateQuestions { get; set; }

	}


}
