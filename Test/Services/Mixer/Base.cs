using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Fritz.StreamTools.Services.Mixer;
using Newtonsoft.Json.Serialization;

namespace Test.Services.Mixer
{
	public abstract class Base
	{
		protected LoggerFactory LoggerFactory { get; }
		protected Lazy<Simulator> SimAuth { get; }
		protected Lazy<Simulator> SimAnon { get; }
		public string Token { get; } = "abcd1234";

		protected Base()
		{
			LoggerFactory = new LoggerFactory();
			LoggerFactory.AddDebug(LogLevel.Trace);

			var configAuth = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>() {
				{ "StreamServices:Mixer:ReconnectDelay", "00:00:00" },
				{ "StreamServices:Mixer:Channel", "MyChannel" },
				{ "StreamServices:Mixer:Token", Token }
			}).Build();
			var configAnon = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>() {
				{ "StreamServices:Mixer:ReconnectDelay", "00:00:00" },
				{ "StreamServices:Mixer:Channel", "MyChannel" }
			}).Build();

			SimAuth = new Lazy<Simulator>(() => new Simulator(configAuth, LoggerFactory));
			SimAnon = new Lazy<Simulator>(() => new Simulator(configAnon, LoggerFactory));
		}

		private static WS.Messages _BuildContentMessages(string text, string link, bool isWhisper)
		{
			var content = new List<WS.Message> {
				new WS.Message { Type = "text", Data = text, Text = text }
			};
			if (link != null)
				content.Add(new WS.Message { Type = "link", Data = link, Text = link });
			var messages = new WS.Messages {
				Message = content
			};
			if (isWhisper)
				messages.Meta = new WS.Meta { Whisper = true };
			return messages;
		}

		protected static string BuildChatMessage(Simulator sim, uint userId, string userName, string text, bool isWhisper = false, string link = null, string[] roles = null, string avatar = null)
		{
			var root = new WS.ChatEvent<WS.ChatData>() {
				Type = "event",
				Event = "ChatMessage",
				Data = new WS.ChatData {
					Channel = sim.ChannelInfo.Id,
					Id = Guid.NewGuid(),
					UserName = userName,
					UserId = userId,
					UserRoles = roles ?? new string[] { "User" },
					UserLevel = 54,
					UserAvatar = avatar,
					Messages = _BuildContentMessages(text, link, isWhisper)
				}
			};
			return MixerSerializer.Serialize(root);
		}

		protected static string BuildTimeoutReply(int id)
		{
			var root = new WS.ChatEvent<string>() {
				Type = "reply",
				Id = id,
				Data = "username has been timed out for some time."
			};
			return MixerSerializer.Serialize(root);
		}

		protected static string BuildMsgReply(Simulator sim, int id, string text, string target = null)
		{
			var root = new WS.ChatEvent<WS.ChatData>() {
				Type = "reply",
				Id = id,
				Data = new WS.ChatData {
					Channel = sim.ChannelInfo.Id,
					Id = Guid.NewGuid(),
					UserName = sim.UserName,
					UserId = sim.ChannelInfo.UserId,
					UserRoles = new string[] { "User" },
					UserLevel = 22,
					UserAvatar = "https://uploads.mixer.com/avatar/ed47s4h5-696.jpg",
					Messages = _BuildContentMessages(text, null, false),
					Target = target
				}
			};
			return MixerSerializer.Serialize(root);
		}

		protected static string BuildUserJoinOrLeave(Simulator sim, string userName, uint userId, bool isJoin)
		{
			var root = new WS.ChatEvent<WS.User>() {
				Type = "event",
				Event = isJoin ? "UserJoin" : "UserLeave",
				Data = new WS.User {
					OriginatingChannel = sim.ChannelInfo.Id,
					Id = userId,
					Username = userName,
					Roles = new string[] { "User" }
				}
			};
			return MixerSerializer.Serialize(root);
		}

		protected static string BuildLiveEvent(string channel, uint? followers = null, uint? viewers = null, bool? online = null)
		{
			var root = new WS.LiveEvent<WS.LivePayload> {
				Type = "event",
				Event = "live",
				Data = new WS.LiveData<WS.LivePayload> {
					Channel = channel,
					Payload = new WS.LivePayload {
						NumFollowers = followers,
						ViewersCurrent = viewers,
						Online = online
					}
				}
			};
			return MixerSerializer.Serialize(root);
		}
	}
}
