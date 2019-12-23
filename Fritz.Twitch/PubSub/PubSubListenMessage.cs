using System;
using System.Collections.Generic;
using System.Text;

namespace Fritz.Twitch.PubSub
{

	public class PubSubListen : PubSubMessage<PubSubListen.PubSubListenData>
	{

		public PubSubListen()
		{
			type = "LISTEN";
		}

		public class PubSubListenData : IPubSubData
		{
			public string[] topics { get; set; }
			public string auth_token { get; set; }
		}



	}


}
