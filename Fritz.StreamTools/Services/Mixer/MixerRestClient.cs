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

		/// <summary>My user name or null if anonymously connected</summary>
		string UserName { get; }
		int UserId { get; }

		/// <summary> Get channel/user info and initializes UserName & UserId properties. Call this first!</summary>
		Task<ChannelInfo> GetChannelInfoAsync();

		Task<int> GetChannelIdAsync();
		Task<ChatAuthKeyAndEndpoints> GetChatAuthKeyAndEndpointsAsync();
		Task<int?> LookupUserIdAsync(string userName);
		Task<bool> BanUserAsync(string userName);
		Task<bool> UnbanUserAsync(string userName);
		Task<DateTimeOffset?> GetStreamStartedAtAsync();
	}

	internal class MixerRestClient : IMixerRestClient
	{
		const string API_URL = "https://mixer.com/api/v1/";

		readonly ILogger _logger;
		readonly HttpClient _client;
		int? _channelId;

		public bool HasToken { get; }
		public string ChannelName { get; }
		public string UserName { get; private set; }
		public int UserId { get; private set; }

		/// <summary>
		/// Construct new MixerRestClient
		/// </summary>
		public MixerRestClient(ILoggerFactory loggerFactory, HttpClient client, string channelName, string token)
		{
			if (loggerFactory == null)
				throw new ArgumentNullException(nameof(loggerFactory));
			if (client == null)
				throw new ArgumentNullException(nameof(client));
			if (string.IsNullOrWhiteSpace(channelName))
				throw new ArgumentException("message", nameof(channelName));

			_logger = loggerFactory.CreateLogger(nameof(MixerRestClient));

			HasToken = !string.IsNullOrEmpty(token);

			_client = client;
			_client.BaseAddress = new Uri(API_URL);
			_client.DefaultRequestHeaders.Add("Accept", "application/json");
			_client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true };
			if (HasToken)
				_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			ChannelName = channelName;
		}

		/// <summary>
		/// Get the channel id number from channel name.
		/// The value is cached after first lookup
		/// </summary>
		public async Task<int> GetChannelIdAsync()
		{
			if (_channelId.HasValue) return _channelId.Value;

			try
			{
				var req = $"channels/{WebUtility.UrlEncode(ChannelName)}?fields=id";
				var doc = await GetJTokenAsync(req);
				_channelId = (int)doc["id"];
				return _channelId.Value;
			}
			catch (HttpRequestException ex)
			{
				throw new UnknownChannelException(ChannelName, ex);
			}
		}

		/// <summary>
		/// Get basic channel information, including current follower and viewer count
		/// </summary>
		public async Task<ChannelInfo> GetChannelInfoAsync()
		{
			try
			{
				var req = $"channels/{WebUtility.UrlEncode(ChannelName)}?fields=id,numFollowers,viewersCurrent";
				var result = await GetAsync<ChannelInfo>(req);
				_channelId = result.Id;

				if (HasToken)
				{
					// User might not be joining own channel
					var me = await GetJTokenAsync("users/current");
					result.UserId = UserId = (int)me["id"];
					UserName = (string)me["username"];
				}

				return result;
			}
			catch (HttpRequestException ex)
			{
				throw new UnknownChannelException(ChannelName, ex);
			}
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
				var doc = await GetJTokenAsync(req);
				return (int)doc["id"];
			}
			catch (HttpRequestException)
			{
				_logger.LogError("Unknown user '{0}'", userName);
				return null;
			}
		}

		/// <summary>
		/// Ban user from chat
		/// </summary>
		public async Task<bool> BanUserAsync(string userName)
		{
			if (string.IsNullOrWhiteSpace(userName))
				throw new ArgumentException("Must not be null or empty", nameof(userName));

			if (!HasToken)
				return false;

			try
			{
				if (string.IsNullOrWhiteSpace(userName))
					throw new ArgumentException("Must not be null or empty", nameof(userName));

				var userId = await LookupUserIdAsync(userName);

				// Add user as banned from our channel
				var req = $"channels/{_channelId}/users/{userId}";
				await PatchAsync(req, new { add = new[] { "Banned" } });
				return true;
			}
			catch (Exception e)
			{
				_logger.LogError("Error banning user '{0}': {1}", userName, e.Message);
				return false;
			}
		}

		/// <summary>
		/// Unban user from chat
		/// </summary>
		public async Task<bool> UnbanUserAsync(string userName)
		{
			if (string.IsNullOrWhiteSpace(userName))
				throw new ArgumentException("Must not be null or empty", nameof(userName));

			if (!HasToken)
				return false;

			try
			{
				var userId = await LookupUserIdAsync(userName);

				// Add user as banned from our channel
				var req = $"channels/{_channelId}/users/{userId}";
				await PatchAsync(req, new { remove = new[] { "Banned" } });
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
			if (_channelId == null) throw new Exception("ChannelId now known! Call GetChannelInfoAsync() first");

			var req = $"channels/{_channelId}/manifest.light2";
			_logger.LogTrace("GET {0}{1}", API_URL, req);
			var response = await _client.GetAsync(req);
			if (response.StatusCode != HttpStatusCode.OK) return null;
			var json = await response.Content.ReadAsStringAsync();
			var doc = JToken.Parse(json);

			if (doc["startedAt"] != null)
			{
				if (DateTime.TryParse((string)doc["startedAt"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var startedAt))
				{
					return startedAt;
				}
			}
			return null;
		}

		/// <summary>
		/// Get auth key and endpoints for connecting websocket to chat
		/// </summary>
		public async Task<ChatAuthKeyAndEndpoints> GetChatAuthKeyAndEndpointsAsync()
		{
			// Get chat authkey and chat endpoints
			var id = await GetChannelIdAsync();
			var req = $"chats/{id}";
			return await GetAsync<ChatAuthKeyAndEndpoints>(req);
		}

		#region HttpClient helpers

		async Task<T> GetAsync<T>(string requestUri)
		{
			_logger.LogTrace("GET {0}{1}", API_URL, requestUri);

			var json = await _client.GetStringAsync(requestUri);
			return JsonConvert.DeserializeObject<T>(json);
		}

		async Task<JToken> GetJTokenAsync(string requestUri)
		{
			_logger.LogTrace("GET {0}{1}", API_URL, requestUri);

			var json = await _client.GetStringAsync(requestUri);
			return JToken.Parse(json);
		}

		async Task PatchAsync<T>(string requestUri, T data)
		{
			_logger.LogTrace("PATCH {0}{1}", API_URL, requestUri);

			var message = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri)
			{
				Content = new JsonContent(data)
			};
			var response = await _client.SendAsync(message);
			response.EnsureSuccessStatusCode();
		}

		#endregion

		public void Dispose()
		{
			// Dont dispose _client here!
			_client?.Dispose();
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
