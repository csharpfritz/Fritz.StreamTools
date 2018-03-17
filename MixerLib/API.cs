using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace MixerLib
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

		public class Preferences
		{
		}

		public class Social
		{
		}

		public class Group
		{
			public uint Id { get; set; }
			public string Name { get; set; }
		}

		public class User
		{
			public uint Level { get; set; }
			public Social Social { get; set; }
			public uint Id { get; set; }
			public string Username { get; set; }
			public bool Verified { get; set; }
			public uint Experience { get; set; }
			public uint Sparks { get; set; }
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
			public uint Id { get; set; }
			public uint UserId { get; set; }
			public string Token { get; set; }
			public bool Online { get; set; }
			public uint FeatureLevel { get; set; }
			public bool Partnered { get; set; }
			public uint? TranscodingProfileId { get; set; }
			public bool Suspended { get; set; }
			public string Name { get; set; }
			public string Audience { get; set; }
			public uint ViewersTotal { get; set; }
			public uint ViewersCurrent { get; set; }
			public uint NumFollowers { get; set; }
			public string Description { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public uint? TypeId { get; set; }
			public bool Interactive { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public uint? InteractiveGameId { get; set; }
			public int Ftl { get; set; }
			public bool HasVod { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public string LanguageId { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public uint? CoverId { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public uint? ThumbnailId { get; set; }
			public uint? BadgeId { get; set; }
			public string BannerUrl { get; set; }
			public uint? HosteeId { get; set; }
			public bool HasTranscodes { get; set; }
			public bool VodsEnabled { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public Guid? CostreamId { get; set; }
			public DateTime CreatedAt { get; set; }
			public DateTime UpdatedAt { get; set; }
			public DateTime? DeletedAt { get; set; }
		}

		public class UserWithChannel : User
		{
			public Channel Channel { get; set; }
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

		public class GameTypeSimple
		{
			public uint Id { get; set; }
			public string Name { get; set; }
			public string CoverUrl { get; set; }
			public string BackgroundUrl { get; set; }
		}
	}
}
