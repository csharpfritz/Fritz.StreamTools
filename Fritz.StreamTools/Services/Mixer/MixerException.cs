using System;
using System.Runtime.Serialization;

namespace Fritz.StreamTools.Services.Mixer
{
	[Serializable]
	internal class MixerException : Exception
	{
		public MixerException()
		{
		}

		public MixerException(string message) : base(message)
		{
		}

		public MixerException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected MixerException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}