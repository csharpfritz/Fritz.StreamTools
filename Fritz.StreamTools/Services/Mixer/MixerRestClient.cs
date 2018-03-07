using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fritz.StreamTools.Helpers;
using Microsoft.Extensions.Logging;

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerRestClient : IDisposable
	{
		bool HasToken { get; }
		string ChannelName { get; }
		uint? ChannelId { get; }

		/// <summary>My user name or null if anonymously connected</summary>
		string UserName { get; }
		uint? UserId { get; }

		/// <summary>
		/// Get initial needed info from the mixer API
		/// </summary>
		/// <param name="channelName">Name of the channel</param>
		/// <param name="oauthToken">The users oauth token or null</param>
		/// <returns>Current number of viewers and followers as a tuple</returns>
		Task<(bool online, int viewers, int followers)> InitAsync(string channelName, string oauthToken);

		Task<API.Chats> GetChatAuthKeyAndEndpointsAsync();
		Task<uint?> LookupUserIdAsync(string userName);
		Task<bool> BanUserAsync(string userName);
		Task<bool> UnbanUserAsync(string userName);
		Task<DateTime?> GetStreamStartedAtAsync();
	}

	internal class MixerRestClient : IMixerRestClient
	{
		const string API_URL = "https://mixer.com/api/v1/";
		readonly ILogger _logger;
		readonly HttpClient _client;
		private bool _initDone;

		public int RetryDelay { get; set; } = 2000;
		public int MaxTries { get; set; } = 3;
		public bool HasToken { get; private set; }
		public string ChannelName { get; private set; }
		public uint? ChannelId { get; private set; }
		public string UserName { get; private set; }
		public uint? UserId { get; private set; }

		/// <summary>
		/// Construct new MixerRestClient
		/// </summary>
		public MixerRestClient(ILoggerFactory loggerFactory, HttpClient client)
		{
			if (loggerFactory == null)
				throw new ArgumentNullException(nameof(loggerFactory));

			_logger = loggerFactory.CreateLogger(nameof(MixerRestClient));

			_client = client ?? throw new ArgumentNullException(nameof(client));
			_client.BaseAddress = new Uri(API_URL);
			_client.DefaultRequestHeaders.Add("Accept", "application/json");
			_client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true };
		}

		public async Task<(bool online, int viewers, int followers)> InitAsync(string channelName, string oauthToken)
		{
			_initDone = false;
			UserId = null;
			ChannelId = 0;
			UserName = null;

			ChannelName = channelName;
			HasToken = !string.IsNullOrEmpty(oauthToken);
			if (HasToken)
				_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oauthToken);
			else
				_client.DefaultRequestHeaders.Authorization = null;

			int tryCounter = 0;
			while (true)
			{
				try
				{
					var req = $"channels/{WebUtility.UrlEncode(ChannelName)}?fields=id,numFollowers,viewersCurrent,online";
					var channelInfo = await GetAsync<API.Channel>(req);
					ChannelId = channelInfo.Id;

					if (HasToken)
					{
						// User might not be joining own channel
						var me = await GetAsync<API.User>("users/current");
						UserId = me.Id;
						UserName = me.Username;
					}
					_initDone = true;
					return (channelInfo.Online, (int)channelInfo.ViewersCurrent, (int)channelInfo.NumFollowers);
				}
				catch (HttpRequestException ex)
				{
					if (++tryCounter == MaxTries)
						throw new MixerException($"Can't find channel '{ChannelName}'", ex);
					await Task.Delay(RetryDelay);
				}
			}
		}

		/// <summary>
		/// Use the REST API to get id of username
		/// </summary>
		/// <param name="userName">Name of the user</param>
		/// <returns>Id of the user</returns>
		public async Task<uint?> LookupUserIdAsync(string userName)
		{
			if (!_initDone)
				throw new Exception("Call InitAsync() first!");

			try
			{
				var req = $"channels/{WebUtility.UrlEncode(userName)}?noCount=1";
				var channel = await GetAsync<API.Channel>(req);
				return channel.UserId;
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
			if (!_initDone)
				throw new Exception("Call InitAsync() first!");

			if (!HasToken)
				return false;

			try
			{
				if (string.IsNullOrWhiteSpace(userName))
					throw new ArgumentException("Must not be null or empty", nameof(userName));

				var userId = await LookupUserIdAsync(userName);

				// Add user as banned from our channel
				var req = $"channels/{ChannelId}/users/{userId}";
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
			if (!_initDone)
				throw new Exception("Call InitAsync() first!");

			if (!HasToken)
				return false;

			try
			{
				var userId = await LookupUserIdAsync(userName);

				// Add user as banned from our channel
				var req = $"channels/{ChannelId}/users/{userId}";
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
		public async Task<DateTime?> GetStreamStartedAtAsync()
		{
			if (!_initDone)
				throw new Exception("Call InitAsync() first!");

			var req = $"channels/{ChannelId}/manifest.light2";
			_logger.LogTrace("GET {0}{1}", API_URL, req);
			var response = await _client.GetAsync(req);
			if (response.StatusCode != HttpStatusCode.OK)
				return null;
			var json = await response.Content.ReadAsStringAsync();
			var manifest = MixerSerializer.Deserialize<API.ChannelManifest2>(json);
			return manifest.StartedAt.ToUniversalTime();
		}

		/// <summary>
		/// Get auth key and endpoints for connecting websocket to chat
		/// </summary>
		public Task<API.Chats> GetChatAuthKeyAndEndpointsAsync()
		{
			if (!_initDone)
				throw new Exception("Call InitAsync() first!");

			// Get chat authkey and chat endpoints
			var req = $"chats/{ChannelId}";
			return GetAsync<API.Chats>(req);
		}

		#region HttpClient helpers

		async Task<T> GetAsync<T>(string requestUri)
		{
			_logger.LogTrace("GET {0}{1}", API_URL, requestUri);

			var json = await _client.GetStringAsync(requestUri);
			return MixerSerializer.Deserialize<T>(json);
		}

		async Task PatchAsync<T>(string requestUri, T data)
		{
			_logger.LogTrace("PATCH {0}{1}", API_URL, requestUri);

			var message = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) {
				Content = new JsonContent(data)
			};
			var response = await _client.SendAsync(message);
			response.EnsureSuccessStatusCode();
		}

		#endregion

		public void Dispose()
		{
			// Don't dispose _client here!
			_client?.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
