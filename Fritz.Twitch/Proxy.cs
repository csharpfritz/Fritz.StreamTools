using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.Twitch
{

	public class Proxy : IDisposable
	{

		private const int MAX_QUEUED = 5;
		public const string LOGGER_CATEGORY = "Fritz.Twitch";

		private ConfigurationSettings Settings { get; }

		private object _QueueLock = new object();
		private int _QueuedRequests = 0;
		private int _WaitingRequests = 0;
		private static short _RateLimitRemaining = 1;
		// Ticks as Volatile.Write doesn't work for DateTime
		private static long _RateLimitResetTicks = DateTime.MaxValue.Ticks;
		private readonly static SemaphoreSlim _RateLimitLock = new SemaphoreSlim(1);

		private static StreamData _CurrentStreamData;
		// Ticks as Volatile.Write doesn't work for DateTime
		private static long _CurrentStreamLastFetchUtcTicks;
		private Timer _FollowersTimer;
		private int _WatchedFollowerCount;
		private int _WatchedViewerCount;
		private Timer _ViewersTimer;
		private readonly static SemaphoreSlim _CurrentStreamLock = new SemaphoreSlim(1);

		private int _WatchViewersIntervalMs;
		private int _WatchFollowersIntervalMs;

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
			await WaitForSlot();

			HttpResponseMessage result;
			await _RateLimitLock.WaitAsync();
			try
			{
				result = await Client.GetAsync(url);

				var remaining = short.Parse(result.Headers.GetValues("RateLimit-Remaining").First());
				var reset = long.Parse(result.Headers.GetValues("RateLimit-Reset").First());

				lock (_QueueLock)
				{
					_RateLimitRemaining = remaining;
					_WaitingRequests--;
					Volatile.Write(ref _RateLimitResetTicks, reset.ToDateTime().Ticks);
				}
				Logger.LogTrace($"{DateTime.UtcNow}: Twitch Rate - {remaining} until {reset.ToDateTime()}");
			}
			finally
			{
				_RateLimitLock.Release();
			}

			result.EnsureSuccessStatusCode();

			return result;

		}

		private async Task WaitForSlot()
		{
			var isQueued = false;
			do
			{
				// Check rate-limit
				lock (_QueueLock)
				{
					if (_RateLimitRemaining - _WaitingRequests > 0)
					{
						_WaitingRequests++;
						if (isQueued)
						{
							_QueuedRequests--;
							isQueued = false;
						}
					}
					else
					{
						if (!isQueued)
						{
							if (_QueuedRequests + 1 > MAX_QUEUED)
							{
								throw new TimeoutException("Too many requests waiting");
							}
							_QueuedRequests++;
							isQueued = true;
						}
					}
				}

				if (isQueued)
				{
					await Task.Delay(new DateTime(Volatile.Read(ref _RateLimitResetTicks)).Subtract(DateTime.UtcNow));
				}
			}
			while (isQueued);
		}

		public async Task<int> GetFollowerCountAsync()
		{

			var url = $"/helix/users/follows?to_id={Settings.UserId}&first=1";
			var result = await GetFromEndpoint(url);

			var resultString = await result.Content.ReadAsStringAsync();
			Logger.LogTrace($"Response from Twitch GetFollowerCount: '{resultString}'");

			return ParseFollowerResult(resultString);
		}

		public async Task<int> GetViewerCountAsync()
		{

			var stream = await GetStreamAsync();
			return (stream?.ViewerCount).GetValueOrDefault(0);

		}

		public void WatchFollowers(int intervalMs = 5000)
		{
			_WatchFollowersIntervalMs = intervalMs;
			_FollowersTimer?.Dispose();

			_FollowersTimer = new Timer(OnWatchFollowers, null, 0, intervalMs);

		}

		private async void OnWatchFollowers(object state)
		{
			// async void as TimerCallback delegate
			try
			{
				// Turn off timer, in case runs longer than interval
				_FollowersTimer?.Change(Timeout.Infinite, Timeout.Infinite);

				// async void as TimerCallback delegate
				var foundFollowerCount = await GetFollowerCountAsync();
				if (foundFollowerCount != _WatchedFollowerCount)
				{
					_WatchedFollowerCount = foundFollowerCount;
					NewFollowers?.Invoke(this, new NewFollowersEventArgs(foundFollowerCount));
				}

				// Turn on timer
				var intervalMs = Math.Max(1000, _WatchFollowersIntervalMs);
				_FollowersTimer?.Change(intervalMs, intervalMs);
			}
			catch (Exception ex)
			{
				// Don't let exception escape from async void
				Logger.LogError($"{DateTime.UtcNow}: OnWatchFollowers - Error {Environment.NewLine}{ex}");
			}
		}

		public void WatchViewers(int intervalMs = 5000)
		{
			_WatchViewersIntervalMs = intervalMs;
			_ViewersTimer?.Dispose();

			_ViewersTimer = new Timer(OnWatchViewers, null, 0, intervalMs);
		}

		private async void OnWatchViewers(object state)
		{
			// async void as TimerCallback delegate
			try
			{
				// Turn off timer, in case runs longer than interval
				_ViewersTimer?.Change(Timeout.Infinite, Timeout.Infinite);

				var foundViewerCount = await GetViewerCountAsync();
				if (foundViewerCount != _WatchedViewerCount)
				{

					_WatchedViewerCount = foundViewerCount;
					NewViewers?.Invoke(this, new NewViewersEventArgs(foundViewerCount));

				}

				// Turn on timer
				var intervalMs = Math.Max(1000, _WatchViewersIntervalMs);
				_ViewersTimer?.Change(intervalMs, intervalMs);
			}
			catch(Exception ex)
			{
				// Don't let exception escape from async void
				Logger.LogError($"{DateTime.UtcNow}: OnWatchViewers - Error {Environment.NewLine}{ex}");
			}

		}

		/// <summary>
		/// Return the duration that the current stream as been airing.  If not currently broadcasting, returns null
		/// </summary>
		public async ValueTask<TimeSpan?> Uptime()
		{
			var startedAt = (await GetStreamAsync())?.StartedAt;
			if (startedAt.HasValue)
			{
				return DateTime.UtcNow.Subtract(startedAt.Value);
			}
			return null;
		}

		public async Task<StreamData> GetStreamAsync()
		{
			if (DateTime.UtcNow.Subtract(new DateTime(Volatile.Read(ref _CurrentStreamLastFetchUtcTicks))) <= TimeSpan.FromSeconds(5) && _CurrentStreamData != null)
			{
				return Volatile.Read(ref _CurrentStreamData);
			}

			if (await _CurrentStreamLock.WaitAsync(5000))
			{
				try
				{
					var url = $"/helix/streams?user_login={Settings.ChannelName}";
					var result = await GetFromEndpoint(url);

					var resultString = await result.Content.ReadAsStringAsync();
					Logger.LogTrace($"Response from Twitch GetStream: '{resultString}'");

					Volatile.Write(ref _CurrentStreamData, ParseStreamResult(resultString));
					Volatile.Write(ref _CurrentStreamLastFetchUtcTicks, DateTime.UtcNow.Ticks);
				}
				finally
				{
					_CurrentStreamLock.Release();
				}
			}

			return Volatile.Read(ref _CurrentStreamData);
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
