using System;
using System.Collections.Generic;
using System.Text;

namespace Fritz.Twitch
{
	public class ConfigurationSettings
	{

		public virtual string ChannelName { get; set; }

		public virtual string ClientId { get; set; }

		public virtual string UserId { get; set; }

		public virtual string ChatBotName { get; set; }

		public virtual string OAuthToken { get; set; }

		[Obsolete]
		public string Channel { get => ChannelName; set => ChannelName = value; }

		[Obsolete]
		public string ChatToken { get => OAuthToken; set => OAuthToken = value; }


	}

}
