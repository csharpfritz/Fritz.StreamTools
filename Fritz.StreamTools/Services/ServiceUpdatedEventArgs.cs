using System;

// REF: https://dev.mixer.com/reference/constellation/index.html

namespace Fritz.StreamTools.Services
{
	public class ServiceUpdatedEventArgs : EventArgs
	{

		public string ServiceName { get; set; }

		public int? NewFollowers { get; set; }

		public int? NewViewers { get; set; }

		public bool? IsOnline { get; set; }
	}

}