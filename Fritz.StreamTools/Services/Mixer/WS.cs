using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Fritz.StreamTools.Services.Mixer
{
	public static class WS
	{
		public class Message
		{
			public string Type { get; set; }
			public string Data { get; set; }
			public string Text { get; set; }
		}

		public class Meta
		{
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public bool? Whisper { get; set; }
		}

		public class Messages
		{
			public IList<Message> Message { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public Meta Meta { get; set; }
		}

		public class Error
		{
			// ???
		}

		public class User
		{
			public uint Id { get; set; }
			public string Username { get; set; }
			public uint OriginatingChannel { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public IList<string> Roles { get; set; }
		}

		public class ChatData
		{
			public uint Channel { get; set; }
			public Guid Id { get; set; }
			[J("user_name")]		public string UserName { get; set; }
			[J("user_id")]			public uint UserId { get; set; }
			[J("user_roles")]		public IList<string> UserRoles { get; set; }
			[J("user_level")]		public uint UserLevel { get; set; }
			[J("user_avatar")]	public string UserAvatar { get; set; }
			[J("message")]			public Messages Messages { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public string Target { get; set; }
		}

		public class LiveData<TPayload>
		{
			[J("channel")] public string Channel { get; set; }
			public TPayload Payload { get; set; }
		}

		public class LivePayload
		{
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public uint? NumFollowers { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public uint? ViewersCurrent { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public bool? Online { get; set; }
			// ... ?
		}

		public class FollowedPayload
		{
			public API.User User { get; set; }
			public bool Following { get; set; }
		}


		public class HostedPayload
		{
			public uint HosterId { get; set; }
			public API.Channel Hoster { get; set; }
		}

		public class SubscribedPayload
		{
			public API.User User { get; set; }
		}

		public class ResubscribedPayload
		{
			public User User { get; set; }
			public DateTime Since { get; set; }
			public DateTime Until { get; set; }
			public uint TotalMonths { get; set; }
		}

		public class LiveEvent
		{
			public string Type { get; set; }
			public string Event { get; set; }
		}

		public class LiveEvent<TPayload> : LiveEvent
		{
			public LiveData<TPayload> Data { get; set; }
		}

		public class HelloData
		{
			public bool Authenticated { get; set; }
		}

		public class ChatEvent
		{
			public string Type { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public string Event { get; set; }
			public Error Error { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public int Id { get; set; }
		}

		public class ChatEvent<TData> : ChatEvent
		{
			public TData Data { get; set; }
		}

		public class Request
		{
			public int Id { get; set; }
			public string Type { get; set; }
			public string Method { get; set; }

			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public object Arguments { get; set; }
			[J(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public object Params { get; set; }
		}
	}
}
