using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fritz.Twitch.PubSub {
	public class PubSubMessageJsonConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(PubSubReceiveMessage<>);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			var jObject = JObject.Load(reader);
			var type = jObject["type"].Value<string>().ToLower();

			if (type == "message") {
				var topicPrefix = jObject["data"]["topic"].Value<string>().TopicToTopicPrefix();
				switch (topicPrefix) {
					case "channel-points-channel-v1":
						return jObject.ToObject<ChannelPointsReceiveMessage>();
					default:
						return null;
				}
			}
			else if (type == "response") {
				return jObject.ToObject<ResponseReceiveMessage>();
			}
			else if (type == "pong") {
				return jObject.ToObject<PongReceiveMessage>();
			}
			else if (type == "reconnect") {
				return jObject.ToObject<ReconnectReceiveMessage>();
			}

			return null;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}
	}


	public class PubSubReceiveMessage<MessageType> : IPubSubReceiveMessage {
		public string Type { get; private set; }

		public MessageData<MessageType> Data { get; private set; }

		public string Error { get; private set; }

		public string TopicPrefix {
			get {
				return Data?.Topic?.TopicToTopicPrefix() ?? string.Empty;
			}
		}
	}

	[JsonConverter(typeof(PubSubMessageJsonConverter))]
	public interface IPubSubReceiveMessage {
		string Type { get; }
		string Error { get; }
		string TopicPrefix { get; }
	}

	public class MessageData<MessageType> {
		public string Topic { get; set; }
		public MessageType Message { get; set; }
	}

	public class ChannelPointsReceiveMessage : PubSubReceiveMessage<ChannelRedemption> { }
	public class ResponseReceiveMessage : PubSubReceiveMessage<string> { }

	public class PongReceiveMessage : PubSubReceiveMessage<string> { }
	public class ReconnectReceiveMessage : PubSubReceiveMessage<string> { }



	public static class PubSubUtils {
		public static string TopicToTopicPrefix(this string fullTopic) {
			var index = fullTopic.IndexOf('.') - 1;
			if (index <= 0) {
				index = fullTopic.Length;
			}

			var topicPrefix = fullTopic.Substring(0, index);
			return topicPrefix;
		}
	}


}
