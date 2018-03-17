using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace MixerLib
{
	public static class MixerSerializer
	{
		static public JsonSerializerSettings Settings { get; } = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
		static public JsonSerializer Serializer { get; } = new JsonSerializer { ContractResolver = Settings.ContractResolver };

		static public T Deserialize<T>(string value) => JsonConvert.DeserializeObject<T>(value, Settings);
		static public string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);

		public static T GetObject<T>(this JToken token) => token.ToObject<T>(Serializer);
	}
}
