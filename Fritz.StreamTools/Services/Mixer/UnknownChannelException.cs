using System;
using System.Runtime.Serialization;

namespace Fritz.StreamTools.Services.Mixer
{
	[Serializable]
	internal class UnknownChannelException : Exception
	{
		public UnknownChannelException()
		{
		}

		public UnknownChannelException(string message) : base(message)
		{
		}

		public UnknownChannelException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected UnknownChannelException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}