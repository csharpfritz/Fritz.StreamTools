using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using Fritz.StreamTools.Helpers;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Test.Services.Mixer
{
	public abstract class Base
	{
		protected LoggerFactory LoggerFactory { get; }
		protected Lazy<Simulator> SimAuth { get; }
		protected Lazy<Simulator> SimAnon { get; }
		protected ITestOutputHelper Output { get; }

		public Base(ITestOutputHelper output)
		{
			Output = output ?? throw new ArgumentNullException(nameof(output));
			LoggerFactory = new LoggerFactory();
			LoggerFactory.AddDebug(LogLevel.Trace);

			var configAuth = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>() {
				{ "StreamServices:Mixer:ReconnectDelay", "00:00:00" },
				{ "StreamServices:Mixer:Channel", "MyChannel" },
				{ "StreamServices:Mixer:Token", "abcd1234" }
			}).Build();
			var configAnon = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>() {
				{ "StreamServices:Mixer:ReconnectDelay", "00:00:00" },
				{ "StreamServices:Mixer:Channel", "MyChannel" }
			}).Build();

			SimAuth = new Lazy<Simulator>(() => new Simulator(configAuth, LoggerFactory, Output));
			SimAnon = new Lazy<Simulator>(() => new Simulator(configAnon, LoggerFactory, Output));
		}

		protected static string BuildChatMessage(Simulator sim, int userId, string userName, string text, string link = null, string[] roles = null, string avatar = null)
		{
			var data = new Packets.ChatMsg {
				type = "event",
				@event = "ChatMessage",
				data = new Packets.ChatMsgData {
					channel = sim.ChannelInfo.Id,
					id = Guid.NewGuid(),
					user_name = userName,
					user_id = userId,
					user_roles = roles ?? new string[] { "User" },
					user_level = 54,
					user_avatar = avatar,
					message = new Packets.ChatMsgMessages {
						message = new Packets.ChatMsgMessage[] {
							new Packets.ChatMsgMessageText {
								type = "text",
								data = text,
								text = text
							}
						}
					}

				}
			};

			if (link != null)
			{
				data.data.message.message = data.data.message.message.Concat(new Packets.ChatMsgMessage[] {
					new Packets.ChatMsgMessageLink {
						type = "link",
						text = link,
						url = link
					}
				}).ToArray();
			}

			return JsonConvert.SerializeObject(data, Formatting.None);
		}

		private static Packets.ChatMsgMessages _BuildContentMessages(string text, string link, bool isWhisper)
		{
			Packets.ChatMsgMessages messages;
			if (!isWhisper)
			{
				messages = new Packets.ChatMsgMessages {
					message = new Packets.ChatMsgMessage[] {
							new Packets.ChatMsgMessageText {
								type = "text",
								data = text,
								text = text
							}
						}
				};
			}
			else
			{
				messages = new Packets.ChatMsgMessagesMeta {
					message = new Packets.ChatMsgMessage[] {
							new Packets.ChatMsgMessageText {
								type = "text",
								data = text,
								text = text
							}
						},
					meta = new Packets.MetaWhisper {
						whisper = true
					}
				};
			}
			if (link != null)
			{
				messages.message = messages.message.Concat(new Packets.ChatMsgMessage[] {
					new Packets.ChatMsgMessageLink {
						type = "link",
						text = link,
						url = link
					}
				}).ToArray();
			}

			return messages;
		}

		protected static string BuildChatWhisper(Simulator sim, int userId, string userName, string text, string link = null, string[] roles = null)
		{
			var data = new Packets.ChatMsg {
				type = "event",
				@event = "ChatMessage",
				data = new Packets.ChatMsgData {
					channel = sim.ChannelInfo.Id,
					id = Guid.NewGuid(),
					user_name = userName,
					user_id = userId,
					user_roles = roles ?? new string[] { "User" },
					user_level = 54,
					user_avatar = null,
					message = _BuildContentMessages(text, link, true)
				}
			};
			return JsonConvert.SerializeObject(data, Formatting.None);
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
						meta = new Packets.Meta {
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
						meta = new Packets.MetaWhisper {
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
