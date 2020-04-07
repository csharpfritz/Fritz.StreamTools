namespace Fritz.Chatbot.QnA.QnAMaker
{
	public class QnAToAdd
	{
		public int id { get; set; }
		public string answer { get; set; }
		public string source { get; set; }
		public string[] questions { get; set; }
		public object[] metadata { get; set; }
		public object[] alternateQuestionClusters { get; set; }
		public QnAToAddContext context { get; set; }
	}


}
