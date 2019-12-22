namespace Fritz.Twitch.PubSub
{

	public abstract class PubSubMessage<DataType> where DataType : IPubSubData
	{

		public string type { get; protected set; }

		public DataType data { get; set; }

	}

	public interface IPubSubData { }


}
