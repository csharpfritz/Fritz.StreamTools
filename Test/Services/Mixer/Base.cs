using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Test.Services.Mixer
{
	public abstract class Base
	{
		protected LoggerFactory LoggerFactory { get; }
		protected Lazy<Simulator> SimAuth { get; }
		protected Lazy<Simulator> SimAnon { get; }

		public Base()
		{
			LoggerFactory = new LoggerFactory();
			var configAuth = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>() {
				{ "StreamServices:Mixer:Channel", "MyChannel" },
				{ "StreamServices:Mixer:Token", "abcd1234" }
			}).Build();
			var configAnon = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>() {
				{ "StreamServices:Mixer:Channel", "MyChannel" }
			}).Build();

			SimAuth = new Lazy<Simulator>(() => new Simulator(configAuth, LoggerFactory));
			SimAnon = new Lazy<Simulator>(() => new Simulator(configAnon, LoggerFactory));
		}


		protected static string BuildMsgReply(Simulator sim, int id, string text)
		{
			var data = new Packets.MsgReply {
				type = "reply",
				id = id,
				error = null,
				data = new Packets.MsgReplyData {
					channel = sim.ChannelInfo.Id,
					id = Guid.NewGuid(),
					user_name = sim.UserName,
					user_id = sim.ChannelInfo.UserId,
					user_level = 22,
					user_avatar = "https://uploads.mixer.com/avatar/ed47s4h5-696.jpg",
					user_roles = new string[] { "User" },
					message = new Packets.MsgReplyMessages {
						message = new Packets.MsgReplyMessage[] {
							new Packets.MsgReplyMessage { type = "text", data = text, text = text }
						},
						meta = new Packets.MsgReplyMeta {
							// empty
						}
					}
				}
			};

			return JsonConvert.SerializeObject(data, Formatting.None);
		}

		protected static string BuildWhisperReply(Simulator sim, int id, string target, string text)
		{
			var data = new Packets.MsgReply {
				type = "reply",
				id = id,
				error = null,
				data = new Packets.MsgReplyDataWhisper {
					channel = sim.ChannelInfo.Id,
					id = Guid.NewGuid(),
					user_name = sim.UserName,
					user_id = sim.ChannelInfo.UserId,
					user_level = 22,
					user_avatar = "https://uploads.mixer.com/avatar/ed47s4h5-696.jpg",
					user_roles = new string[] { "User" },
					message = new Packets.MsgReplyMessages {
						message = new Packets.MsgReplyMessage[] {
							new Packets.MsgReplyMessage { type = "text", data = text, text = text }
						},
						meta = new Packets.MsgReplyMetaWhisper {
							whisper = true
						}
					},
					target = target
				}
			};

			return JsonConvert.SerializeObject(data, Formatting.None);
		}
	}
}
