using System;

namespace Fritz.Twitch.PubSub
{

	public abstract class PubSubMessage<DataType> where DataType : IPubSubData
	{

		public string type { get; protected set; }

		public string nonce { get; set; } = Guid.NewGuid().ToString().Replace("-", "");

		public DataType data { get; set; }

	}

	public interface IPubSubData { }


}
