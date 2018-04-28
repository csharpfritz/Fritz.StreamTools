using Newtonsoft.Json.Linq;
using System;

namespace Fritz.Twitch
{
	public class StreamData
	{

		public long Id { get; set; }

		public long UserId { get; set; }

		public long GameId { get; set; }

		public string Type { get; set; }

		public string Title { get; set; }

		public int ViewerCount { get; set; }

		public DateTime StartedAt { get; set; }

		public string Language { get; set; }

		public static explicit operator StreamData(JToken obj)
		{

			return new StreamData
			{
				Id = obj["id"].Value<long>(),
				UserId = obj["user_id"].Value<long>(),
				GameId = obj["game_id"].Value<long>(),
				Type = obj["type"].Value<string>(),
				Title = obj["title"].Value<string>(),
				ViewerCount = obj["viewer_count"].Value<int>(),
				StartedAt = obj["started_at"].Value<DateTime>(),
				Language = obj["language"].Value<string>()
			};

		}

	}

}
