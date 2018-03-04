using System;
using System.Collections.Generic;

namespace Fritz.StreamTools.Services.Mixer
{
	public static class API
	{
		//
		// https://mixer.com/api/v1/channels/<ChannelId>/manifest.light2
		//
		public class ChannelManifest2
		{
			public DateTime Now { get; set; }
			public bool IsTestStream { get; set; }
			public DateTime StartedAt { get; set; }
			public string AccessKey { get; set; }
			public string HlsSrc { get; set; }
		}

		//
		// https://mixer.com/api/v1/channels/<ChannelName>
		//
		public class Type
		{
			public int Id { get; set; }
			public string Name { get; set; }
			public string Parent { get; set; }
			public string Description { get; set; }
			public string Source { get; set; }
			public int ViewersCurrent { get; set; }
			//public object CoverUrl { get; set; }
			//public object BackgroundUrl { get; set; }
			public int Online { get; set; }
			//public object AvailableAt { get; set; }
		}

		public class Preferences
		{
			public string CostreamAllow { get; set; }
			public string Sharetext { get; set; }
			//public IList<object> ChannelBannedwords { get; set; }
			public bool ChannelLinksClickable { get; set; }
			public bool ChannelLinksAllowed { get; set; }
			public int ChannelSlowchat { get; set; }
			public bool ChannelNotifyFollow { get; set; }
			public string ChannelNotifyFollowmessage { get; set; }
			public string ChannelNotifyHostedBy { get; set; }
			public string ChannelNotifyHosting { get; set; }
			public string ChannelNotifySubscribemessage { get; set; }
			public bool ChannelNotifySubscribe { get; set; }
			public string ChannelPartnerSubmail { get; set; }
			public bool ChannelPlayerMuteOwn { get; set; }
			public bool ChannelTweetEnabled { get; set; }
			public string ChannelTweetBody { get; set; }
			public int ChannelUsersLevelRestrict { get; set; }
			public int ChannelCatbotLevel { get; set; }
			public bool ChannelOfflineAutoplayVod { get; set; }
			public bool ChannelChatHostswitch { get; set; }
		}

		public class Social
		{
			//public IList<object> Verified { get; set; }
		}

		public class Group
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		public class User
		{
			public int Level { get; set; }
			public Social Social { get; set; }
			public int Id { get; set; }
			public string Username { get; set; }
			public bool Verified { get; set; }
			public int Experience { get; set; }
			public int Sparks { get; set; }
			public string AvatarUrl { get; set; }
			//public object Bio { get; set; }
			//public object PrimaryTeam { get; set; }
			public DateTime CreatedAt { get; set; }
			public DateTime UpdatedAt { get; set; }
			public DateTime? DeletedAt { get; set; }
			public IList<Group> Groups { get; set; }
		}

		public class Channel
		{
			public bool Featured { get; set; }
			public int Id { get; set; }
			public int UserId { get; set; }
			public string Token { get; set; }
			public bool Online { get; set; }
			public int FeatureLevel { get; set; }
			public bool Partnered { get; set; }
			//public object TranscodingProfileId { get; set; }
			public bool Suspended { get; set; }
			public string Name { get; set; }
			public string Audience { get; set; }
			public int ViewersTotal { get; set; }
			public int ViewersCurrent { get; set; }
			public int NumFollowers { get; set; }
			public string Description { get; set; }
			public int TypeId { get; set; }
			public bool Interactive { get; set; }
			//public object InteractiveGameId { get; set; }
			public int Ftl { get; set; }
			public bool HasVod { get; set; }
			//public object LanguageId { get; set; }
			//public object CoverId { get; set; }
			//public object ThumbnailId { get; set; }
			//public object BadgeId { get; set; }
			//public object BannerUrl { get; set; }
			//public object HosteeId { get; set; }
			public bool HasTranscodes { get; set; }
			public bool VodsEnabled { get; set; }
			//public object CostreamId { get; set; }
			public DateTime CreatedAt { get; set; }
			public DateTime UpdatedAt { get; set; }
			//public object DeletedAt { get; set; }
			//public object Thumbnail { get; set; }
			//public object Cover { get; set; }
			//public object Badge { get; set; }
			public Type Type { get; set; }
			public Preferences Preferences { get; set; }
			public User User { get; set; }
		}

		//
		// https://mixer.com/api/v1/chats/<ChannelId>
		//

		public class Chats
		{
			public IList<string> Roles { get; set; }
			public string Authkey { get; set; }
			public IList<string> Permissions { get; set; }
			public IList<string> Endpoints { get; set; }
			public bool IsLoadShed { get; set; }
		}
	}
}