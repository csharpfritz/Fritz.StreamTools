using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.Twitch
{

	public class Proxy : IDisposable
	{

		public const string LOGGER_CATEGORY = "Fritz.Twitch";

		private ConfigurationSettings Settings { get; }

		private static short _RateLimitRemaining = 1;
		private static DateTime _RateLimitReset = DateTime.MaxValue;
		private readonly static SemaphoreSlim _RateLimitLock = new SemaphoreSlim(1);

		private static StreamData _CurrentStreamData;
		private static DateTime _CurrentStreamLastFetchUtc;
		private Timer _FollowersTimer;
		private int _WatchedFollowerCount;
		private int _WatchedViewerCount;
		private Timer _ViewersTimer;
		private readonly static SemaphoreSlim _CurrentStreamLock = new SemaphoreSlim(1);

		private ILogger Logger { get; }
		internal HttpClient Client { get; private set; }

		public event EventHandler<NewFollowersEventArgs> NewFollowers;
		public event EventHandler<NewViewersEventArgs> NewViewers;

		/// <summary>
		/// Create a proxy for use in managing connection to Twitch
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="loggerFactory">Create a logger and connect to the 'Fritz.Twitch' category</param>
		public Proxy(HttpClient client, IOptions<ConfigurationSettings> settings, ILoggerFactory loggerFactory) :
			this(client, settings.Value, loggerFactory.CreateLogger(LOGGER_CATEGORY))
		{

		}

		internal Proxy(HttpClient client, ConfigurationSettings settings, ILogger logger)
		{

			Settings = settings;
			Logger = logger;

			ConfigureClient(client);

		}

		private void ConfigureClient(HttpClient client)
		{

			client.BaseAddress = new Uri("https://api.twitch.tv");
			client.DefaultRequestHeaders.Add("Client-ID", Settings.ClientId);

			this.Client = client;

		}

		private async Task<HttpResponseMessage> GetFromEndpoint(string url)
		{

			// Check rate-limit
			await _RateLimitLock.WaitAsync();
			if (_RateLimitRemaining <= 0)
			{
				_RateLimitLock.Release();
				await Task.Delay(_RateLimitReset.Subtract(DateTime.UtcNow));
				return await GetFromEndpoint(url);
			}
			_RateLimitLock.Release();

			var result = await Client.GetAsync(url);

			var remaining = short.Parse(result.Headers.GetValues("RateLimit-Remaining").First());
			var reset = long.Parse(result.Headers.GetValues("RateLimit-Reset").First());

			await _RateLimitLock.WaitAsync();
			_RateLimitRemaining = remaining;
			_RateLimitReset = reset.ToDateTime();
			Logger.LogTrace($"{DateTime.UtcNow}: Twitch Rate - {remaining} until {_RateLimitReset}");
			_RateLimitLock.Release();

			result.EnsureSuccessStatusCode();

			return result;

		}

		public async Task<int> GetFollowerCountAsync()
		{

			var url = $"/helix/users/follows?to_id={Settings.UserId}&first=1";
			var result = await GetFromEndpoint(url);

			var resultString = await result.Content.ReadAsStringAsync();
			Logger.LogTrace($"Response from Twitch GetFollowerCount: '{resultString}'");

			return ParseFollowerResult(resultString);

		}

		public int GetFollowerCount()
		{
			return GetFollowerCountAsync().GetAwaiter().GetResult();
		}

		public async Task<int> GetViewerCountAsync()
		{

			var stream = await GetStreamAsync();
			return (stream?.ViewerCount).GetValueOrDefault(0);

		}

		public int GetViewerCount()
		{
			return GetViewerCountAsync().GetAwaiter().GetResult();
		}

		public void WatchFollowers(int intervalMs = 5000)
		{

			_FollowersTimer = new Timer(OnWatchFollowers, null, 0, intervalMs);

		}

		private void OnWatchFollowers(object state)
		{

			var foundFollowerCount = GetFollowerCount();
			if (foundFollowerCount != _WatchedFollowerCount)
			{
				_WatchedFollowerCount = foundFollowerCount;
				NewFollowers?.Invoke(this, new NewFollowersEventArgs(foundFollowerCount));
			}

		}

		public void WatchViewers(int intervalMs = 5000)
		{
			_ViewersTimer = new Timer(OnWatchViewers, null, 0, intervalMs);
		}

		private void OnWatchViewers(object state)
		{

			var foundViewerCount = GetViewerCount();
			if (foundViewerCount != _WatchedViewerCount)
			{

				_WatchedViewerCount = foundViewerCount;
				NewViewers?.Invoke(this, new NewViewersEventArgs(foundViewerCount));

			}

		}

		/// <summary>
		/// Return the duration that the current stream as been airing.  If not currently broadcasting, returns null
		/// </summary>
		public TimeSpan? Uptime
		{
			get
			{
				var startedAt = GetStreamAsync().GetAwaiter().GetResult()?.StartedAt;
				if (startedAt.HasValue)
				{
					return DateTime.UtcNow.Subtract(startedAt.Value);
				}
				return null;
			}
		}

		public async Task<StreamData> GetStreamAsync()
		{

			await _CurrentStreamLock.WaitAsync();
			if (DateTime.UtcNow.Subtract(_CurrentStreamLastFetchUtc) <= TimeSpan.FromSeconds(5) && _CurrentStreamData != null)
			{
				var outData = _CurrentStreamData;
				_CurrentStreamLock.Release();
				return outData;
			}

			_CurrentStreamLock.Release();
			if (await _CurrentStreamLock.WaitAsync(5000))
			{

				var url = $"/helix/streams?user_login={Settings.ChannelName}";
				var result = GetFromEndpoint(url).GetAwaiter().GetResult();

				var resultString = await result.Content.ReadAsStringAsync();
				Logger.LogTrace($"Response from Twitch GetStream: '{resultString}'");

				_CurrentStreamData = ParseStreamResult(resultString);
				_CurrentStreamLastFetchUtc = DateTime.UtcNow;

				_CurrentStreamLock.Release();

			}

			return _CurrentStreamData;

		}

		internal static int ParseFollowerResult(string twitchString)
		{

			var jObj = JsonConvert.DeserializeObject<JObject>(twitchString);

			return jObj.Value<int>("total");

		}

		internal static StreamData ParseStreamResult(string twitchString)
		{

			var jObj = JsonConvert.DeserializeObject<JObject>(twitchString);

			if (!jObj["data"].HasValues)
			{
				return null;
			}

			var data = jObj.GetValue("data")[0];

			return (StreamData)data;

		}

		public void Dispose()
		{
			if (_FollowersTimer != null)
			{
				_FollowersTimer.Dispose();
			}
		}
	}

}
