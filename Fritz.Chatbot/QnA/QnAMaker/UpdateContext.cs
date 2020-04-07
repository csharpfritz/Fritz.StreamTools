namespace Fritz.Chatbot.QnA.QnAMaker
{
	public class UpdateContext
	{
		public bool isContextOnly { get; set; }
		public Promptstoadd[] promptsToAdd { get; set; }
		public int[] promptsToDelete { get; set; }
	}


}
