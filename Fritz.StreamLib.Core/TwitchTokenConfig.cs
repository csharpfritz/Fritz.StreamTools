using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fritz.StreamLib.Core
{
	public class TwitchTokenConfig
	{
		private readonly HttpClient _Client;
		private readonly IConfiguration _Configuration;
		private readonly ILogger _Logger;

		public TwitchTokenConfig(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILoggerFactory loggerFactory)
		{

			_Client = httpClientFactory.CreateClient("TwitchToken");
			_Configuration = configuration;
			_Logger = loggerFactory.CreateLogger("TwitchToken");

			GetNewToken().GetAwaiter().GetResult();

		}
		public static TwitchTokenResponse Tokens { get; private set; }

		private async Task GetNewToken()
		{

			var sb = new StringBuilder("https://id.twitch.tv/oauth2/token");
			sb.Append($"?client_id={_Configuration["StreamServices:Twitch:ClientId"]}");
			sb.Append($"&client_secret={_Configuration["StreamServices:Twitch:ClientAccessToken"]}");
			sb.Append($"&grant_type=client_credentials");
			sb.Append($"&scope=");

			var result = await _Client.PostAsync(sb.ToString(), null);

			try
			{
				result.EnsureSuccessStatusCode();
			} catch (Exception e) {
				_Logger.LogError("Error while fetching access tokens: " + e);
				return;
			}
			var json = await result.Content.ReadAsStringAsync();
			TwitchTokenConfig.Tokens = JsonSerializer.Deserialize<TwitchTokenResponse>(json);

		}

	}


	public class TwitchTokenResponse
	{

		private DateTime _CreateDate = DateTime.Now;

		public string access_token { get; set; }
		public string refresh_token { get; set; }
		public int expires_in { get; set; }
		public string[] scope { get; set; }
		public string token_type { get; set; }

		public DateTime Expiration {  get { return _CreateDate.Add(TimeSpan.FromSeconds(expires_in)); } }

	}


}
