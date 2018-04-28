using Fritz.StreamLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Helpers
{
	public static class ChatEventArgsHelper
	{

		public static ChatMessageEventArgs FromMixerLib(this ChatMessageEventArgs outArgs, MixerLib.Events.ChatMessageEventArgs args)
		{

			outArgs.ChannelId = args.ChannelId;
			outArgs.IsModerator = args.IsModerator;
			outArgs.IsOwner = args.IsOwner;
			outArgs.IsWhisper = args.IsWhisper;
			outArgs.Message = args.Message.Replace("( Removed by CatBot )", "");
			outArgs.ServiceName = "Mixer";
			outArgs.UserId = args.UserId;
			outArgs.UserName = args.UserName;

			return outArgs;

		}

		public static ChatUserInfoEventArgs FromMixerLib(this ChatUserInfoEventArgs outArgs, MixerLib.Events.ChatUserInfoEventArgs args)
		{

			outArgs.ChannelId = args.ChannelId;
			outArgs.UserName = args.UserName;
			outArgs.UserId = args.UserId;

			return outArgs;

		}

	}

}
