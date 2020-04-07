namespace Fritz.Chatbot.QnA.QnAMaker
{
	public class UpdateQnAList
	{
		public int id { get; set; }
		public string answer { get; set; }
		public string source { get; set; }
		public QuestionChangeOperations questions { get; set; }
		public UpdateMetadata metadata { get; set; }
		public Alternatequestionclusters alternateQuestionClusters { get; set; }
		public UpdateContext context { get; set; }
	}


}
