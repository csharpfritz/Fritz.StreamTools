using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fritz.StreamTools.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerRestClient : IDisposable
	{
		bool HasToken { get; }
		string ChannelName { get; }

		Task<int> GetChannelIdAsync();
		Task<ChannelInfo> GetChannelInfoAsync();
		Task<ChatAuthKeyAndEndpoints> GetChatAuthKeyAndEndpointsAsync();
		Task<int?> LookupUserIdAsync(string userName);
		Task<bool> BanUserAsync(string userName);
		Task<bool> UnbanUserAsync(string userName);
		Task<DateTimeOffset?> GetStreamStartedAtAsync();
	}

	internal class MixerRestClient : IMixerRestClient
	{
		const string API_URL = "https://mixer.com/api/v1/";

		readonly HttpClient _client;
		readonly string _channelName;
		int? _channelId;
		private ILogger _logger;

		public bool HasToken { get; }
		public string ChannelName { get => _channelName; }

		public MixerRestClient(ILoggerFactory loggerFactory, string channelName, string token)
		{
			if (loggerFactory == null)
				throw new ArgumentNullException(nameof(loggerFactory));
			if (string.IsNullOrWhiteSpace(channelName))
				throw new ArgumentException("message", nameof(channelName));

			_logger = loggerFactory.CreateLogger(nameof(MixerRestClient));

			HasToken = !string.IsNullOrEmpty(token);

			_client = new HttpClient { BaseAddress = new Uri(API_URL) };
			_client.DefaultRequestHeaders.Add("Accept", "application/json");
			_client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true };
			if (HasToken)
				_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			_channelName = channelName;
		}

		public async Task<int> GetChannelIdAsync()
		{
			if (_channelId.HasValue) return _channelId.Value;

			var req = $"channels/{WebUtility.UrlEncode(_channelName)}?fields=id";
			_logger.LogTrace("GET {0}{1}", API_URL, req);
			var json = await _client.GetStringAsync(req).ConfigureAwait(false);
			_channelId = JObject.Parse(json)["id"].Value<int>();
			return _channelId.Value;
		}

		public async Task<ChannelInfo> GetChannelInfoAsync()
		{
			var req = $"channels/{WebUtility.UrlEncode(_channelName)}?fields=id,userId,numFollowers,viewersCurrent";
			_logger.LogTrace("GET {0}{1}", API_URL, req);
			var json = await _client.GetStringAsync(req).ConfigureAwait(false);
			var result = JsonConvert.DeserializeObject<ChannelInfo>(json);
			_channelId = result.Id;
			return result;
		}

		/// <summary>
		/// Use the REST API to get id of username
		/// </summary>
		/// <param name="userName">Name of the user</param>
		/// <returns>Id of the user</returns>
		public async Task<int?> LookupUserIdAsync(string userName)
		{
			try
			{
				var req = $"channels/{WebUtility.UrlEncode(userName)}?noCount=1";
				_logger.LogTrace("GET {0}{1}", API_URL, req);
				var json = await _client.GetStringAsync(req);
				var doc = JToken.Parse(json);
				var userId = (int)doc["id"];
				return userId;

			}
			catch (Exception e)
			{
				_logger.LogError("Cant find user '0': {1}", userName, e.Message);
				return null;
			}
		}

		public async Task<bool> BanUserAsync(string userName)
		{
			if (string.IsNullOrWhiteSpace(userName))
				throw new ArgumentException("Must not be null or empty", nameof(userName));

			try
			{
				if (string.IsNullOrWhiteSpace(userName))
					throw new ArgumentException("Must not be null or empty", nameof(userName));

				var userId = await LookupUserIdAsync(userName);

				// Add user as banned from our channel
				var req = $"channels/{_channelId}/users/{userId}";
				_logger.LogTrace("PATCH {0}{1}", API_URL, req);
				var message = new HttpRequestMessage(new HttpMethod("PATCH"), req)
				{
					Content = new JsonContent(new { add = new[] { "Banned" } })
				};
				var response = await _client.SendAsync(message);
				response.EnsureSuccessStatusCode();
				return true;

			}
			catch (Exception e)
			{
				_logger.LogError("Error banning user '{0}': {1}", userName, e.Message);
				return false;
			}
		}

		public async Task<bool> UnbanUserAsync(string userName)
		{
			if (string.IsNullOrWhiteSpace(userName))
				throw new ArgumentException("Must not be null or empty", nameof(userName));

			try
			{
				var userId = await LookupUserIdAsync(userName);

				// Add user as banned from our channel
				var req = $"channels/{_channelId}/users/{userId}";
				_logger.LogTrace("PATCH {0}{1}", API_URL, req);
				var message = new HttpRequestMessage(new HttpMethod("PATCH"), req)
				{
					Content = new JsonContent(new { remove = new[] { "Banned" } })
				};
				var response = await _client.SendAsync(message);
				response.EnsureSuccessStatusCode();
				return true;
			}
			catch (Exception e)
			{
				_logger.LogError("Error unbanning user '{0}': {1}", userName, e.Message);
				return false;
			}
		}

		/// <summary>
		/// Get stream start time from REST API
		/// </summary>
		/// <returns>Start time of stream, or null if stream is offline</returns>
		public async Task<DateTimeOffset?> GetStreamStartedAtAsync()
		{
			var req = $"channels/{_channelId}/manifest.light2";
			_logger.LogTrace("GET {0}{1}", API_URL, req);
			var response = await _client.GetAsync(req);
			if (response.StatusCode != HttpStatusCode.OK) return null;
			var json = await response.Content.ReadAsStringAsync();
			var doc = JToken.Parse(json);

			if (doc["startedAt"] != null)
			{
				if (DateTimeOffset.TryParse((string)doc["startedAt"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var startedAt))
				{
					return startedAt;
				}
			}
			return null;
		}

		public async Task<ChatAuthKeyAndEndpoints> GetChatAuthKeyAndEndpointsAsync()
		{
			// Get chat authkey and chat endpoints
			var id = await GetChannelIdAsync();
			var req = $"chats/{id}";
			_logger.LogTrace("GET {0}{1}", API_URL, req);
			var json = await _client.GetStringAsync(req);
			return JsonConvert.DeserializeObject<ChatAuthKeyAndEndpoints>(json);
		}

		public void Dispose()
		{
			// Dont dispose _client here!
			_client.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	public class ChatAuthKeyAndEndpoints
	{
		[JsonProperty("authkey")]
		public string AuthKey { get; set; }

		[JsonProperty("endpoints")]
		public string[] Endpoints { get; set; }
	}

	public class ChannelInfo
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("userId")]
		public int UserId { get; set; }

		[JsonProperty("numFollowers")]
		public int NumberOfFollowers { get; set; }

		[JsonProperty("viewersCurrent")]
		public int NumberOfViewers { get; set; }
	}
}
