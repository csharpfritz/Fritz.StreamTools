using FaunaDB.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using static FaunaDB.Query.Language;
using static FaunaDB.Types.Option;
using static FaunaDB.Types.Encoder;
using FaunaDB.Types;

namespace Fritz.Chatbot
{
	public class HatDescriptionRepository
	{
		private readonly FaunaClient _Client;

		public HatDescriptionRepository(IConfiguration configuration)
		{

			var secret = configuration["FaunaDb:Secret"];

			_Client = new FaunaClient(secret);

		}

		/// <summary>
		/// Get the description, if any, that goes with the tag for the hat identified
		/// </summary>
		/// <param name="tag">The unique tag for the hat</param>
		/// <returns>Description (if any) for the hat</returns>
		public async Task<HatData> GetHatData(string tag) {

			try
			{
				var singleMatch = await _Client.Query(Get(Match(Index("hats_tag_desc"), tag)));

				var hatData = singleMatch.Get(Field.At("data")).To<HatData>().Value;
				return hatData;
			} catch {
				// No result found
				return null;
			}
		}


		public class HatData {

			[FaunaField("tag")]
			public string Tag { get; set; }

			[FaunaField("description")]
			public string Description { get; set; }

			[FaunaField("name")]
			public string? Name { get; set; }

		}

	}

}
