using System.ComponentModel.DataAnnotations;

namespace Fritz.Chatbot.QnA.Data
{
	public class AlternateQuestion {

		public int Id { get; set; }

		[Required]
		public int QuestionId { get; set; }

		[Required]
		[MaxLength(280)]    // Same length as a Tweet
		public string QuestionText { get; set; }

		public QnAPair MainQuestion { get; set; }

	}


}
