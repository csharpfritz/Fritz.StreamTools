using System;
using System.Runtime.Serialization;

namespace Fritz.Twitch.PubSub
{
	[Serializable]
	internal class UnhandledPubSubMessageException : Exception
	{
		public UnhandledPubSubMessageException()
		{
		}

		public UnhandledPubSubMessageException(string message) : base(message)
		{
		}

		public UnhandledPubSubMessageException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected UnhandledPubSubMessageException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}