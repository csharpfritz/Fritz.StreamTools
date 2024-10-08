using System;
using System.Text.Json.Nodes;

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

		public static explicit operator StreamData(JsonNode obj)
		{

			return new StreamData
			{
				Id = obj["id"].GetValue<long>(),
				UserId = obj["user_id"].GetValue<long>(),
				GameId = obj["game_id"].GetValue<long>(),
				Type = obj["type"].GetValue<string>(),
				Title = obj["title"].GetValue<string>(),
				ViewerCount = obj["viewer_count"].GetValue<int>(),
				StartedAt = obj["started_at"].GetValue<DateTime>(),
				Language = obj["language"].GetValue<string>()
			};

		}

	}

}
