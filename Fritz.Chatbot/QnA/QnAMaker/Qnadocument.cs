namespace Fritz.Chatbot.QnA.QnAMaker
{
	public class Qnadocument
	{
		public int id { get; set; }
		public string answer { get; set; }
		public string source { get; set; }
		public string[] questions { get; set; }
		public Metadata[] metadata { get; set; }
	}



}
