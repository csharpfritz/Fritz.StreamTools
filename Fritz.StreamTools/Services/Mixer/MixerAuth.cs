using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// https://dev.mixer.com/reference/oauth/index.html
// https://dev.mixer.com/rest.html#oauth_shortcode_post

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerAuth
	{
		string AccessToken { get; }

		Task<bool> DoShortCodeAuthAsync();
		void EnsureTokenRefresherStarted();
		Task RefreshTokenIfNeeded();
	}

	public class MixerAuth : IMixerAuth
  {
		const string TOKEN_FILENAME = "fritz_streamtools_mixer_token.json";
		const string REQUIRED_OAUTH_SCOPES = "chat:chat chat:connect chat:whisper chat:change_ban chat:timeout";

		object _tokenLock = new object();
		OAuthToken _token;
		IConfiguration _config;
		ILogger _logger;
		HttpClient _client;
		Task _tokenRefresherTask;
		CancellationToken _shutdownRequested;

		public MixerAuth(IConfiguration config,  ILoggerFactory loggerFactory, HttpClient client, CancellationToken shutdown)
		{
			_config = config;
			_logger = loggerFactory.CreateLogger<MixerAuth>();
			_client = client;
			_shutdownRequested = shutdown;

			_token = LoadToken();
		}

		/// <summary>
		/// 
		/// </summary>
		public string AccessToken
		{
			get
			{
				lock (_tokenLock)
				{
					return _token?.AccessToken;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public async Task<bool> DoShortCodeAuthAsync()
		{
			var client = _client;

			try
			{
				// Send short code request to mixer api
				var data = new
				{
					client_id = _config["StreamServices:Mixer:ClientId"],
					client_secret = _config["StreamServices:Mixer:ClientSecret"],
					scope = REQUIRED_OAUTH_SCOPES
				};
				var result = await client.PostAsync("oauth/shortcode", new JsonContent(data));
				var json = await result.Content.ReadAsStringAsync();
				var doc = JObject.Parse(json);

				var code = doc["code"].Value<string>();
				var handle = doc["handle"].Value<string>();
				var expiresIn = doc["expires_in"].Value<int>();
				var expireTime = DateTime.UtcNow + TimeSpan.FromSeconds(expiresIn);

				// Show code to user
				var prevColor = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"Go to 'https://mixer.com/go' and enter code {code} within {expiresIn} seconds");
				Console.ForegroundColor = prevColor;

				// Poll mixer api for user accept, and get auth code
				string authCode = null;
				while (expireTime > DateTime.UtcNow)
				{
					await Task.Delay(500);
					result = await client.GetAsync($"oauth/shortcode/check/{handle}");
					if (result.StatusCode == System.Net.HttpStatusCode.OK)
					{
						json = await result.Content.ReadAsStringAsync();
						doc = JObject.Parse(json);
						authCode = doc["code"].Value<string>();
						break;
					}
				}

				if (string.IsNullOrEmpty(authCode))
				{
					_logger.LogError($"Mixer auth failed: {result.StatusCode}");
					return false;
				}

				// Call mixer token endpoint to login
				var authData = new
				{
					grant_type = "authorization_code",
					client_id = _config["StreamServices:Mixer:ClientId"],
					client_secret = _config["StreamServices:Mixer:ClientSecret"],
					code = authCode
				};
				result = await client.PostAsync("oauth/token", new JsonContent(authData));
				result.EnsureSuccessStatusCode();
				json = await result.Content.ReadAsStringAsync();
				lock (_tokenLock)
				{
					_token = OAuthToken.Parse(json);
					StoreToken(_token);
				}

				_logger.LogInformation($"Mixer auth succeeded. Access token valid until {_token.ValidUntil} UTC");

				return true;
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Mixer shortcode auth failed");
				return false;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void Stop()
		{

		}

		/// <summary>
		/// 
		/// </summary>
		public void EnsureTokenRefresherStarted()
		{
			// Start automaticly refreshing access_token
			if (_tokenRefresherTask == null) _tokenRefresherTask = Task.Run(TokenRefresher);
		}

		/// <summary>
		/// 
		/// </summary>
		private async Task TokenRefresher()
		{
			try
			{
				while (!_shutdownRequested.IsCancellationRequested)
				{
					// Refresh token before it expires
					var delay = TimeSpan.FromSeconds((int)((_token.ValidUntil - DateTime.UtcNow).TotalSeconds * .9));
					if (delay.TotalSeconds < 0) delay = TimeSpan.Zero;
					if (delay != TimeSpan.Zero)
					{
						_logger.LogInformation($"Refreshing mixer access_token in {delay.ToString()}");
						await Task.Delay(delay, _shutdownRequested);
					}

					await RefreshToken();
				}
			}
			catch (OperationCanceledException)
			{
				// Shutdown requested
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Mixer access_token refreshed failed. You need to restart app to authenticate");
				_token = null;
			}

			_tokenRefresherTask = null;
		}

		private async Task RefreshToken()
		{
			// Refresh token
			_logger.LogInformation("Refreshing access_token");
			var refreshData = new
			{
				grant_type = "refresh_token",
				refresh_token = _token.RefreshToken,
				client_id = _config["StreamServices:Mixer:ClientId"],
				client_secret = _config["StreamServices:Mixer:ClientSecret"]
			};
			var result = await _client.PostAsync("oauth/token", new JsonContent(refreshData));
			result.EnsureSuccessStatusCode();
			var json = await result.Content.ReadAsStringAsync();
			lock (_tokenLock)
			{
				_token = OAuthToken.Parse(json);
				StoreToken(_token);
			}

			_logger.LogInformation("Mixer access_token refreshed");
		}

		/// <summary>
		/// If the token is about to expire, then refresh it
		/// </summary>
		public async Task RefreshTokenIfNeeded()
		{
			if (_token == null) return;
			var refreshInSecondes = (int)((_token.ValidUntil - DateTime.UtcNow).TotalSeconds * .9);
			if (refreshInSecondes > 0) return;  // Not expired yet

			await RefreshToken();
		}

		private class OAuthToken
		{
			public static OAuthToken Parse(string json)
			{
				var doc = JToken.Parse(json);
				return new OAuthToken
				{
					AccessToken = doc["access_token"].Value<string>(),
					RefreshToken = doc["refresh_token"].Value<string>(),
					ValidUntil = DateTime.UtcNow.AddSeconds(doc["expires_in"].Value<int>())
				};
			}

			public void Update(string newAccessToken, int expiresIn)
			{
				AccessToken = newAccessToken;
				ValidUntil = DateTime.UtcNow.AddSeconds(expiresIn);
			}

			public string AccessToken { get; set; }
			public DateTime ValidUntil { get; set; }
			public string RefreshToken { get; set; }
		}

		/// <summary>
		/// 
		/// </summary>
		private void StoreToken(OAuthToken token)
		{
			var filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), TOKEN_FILENAME);

			try
			{
				var json = JsonConvert.SerializeObject(_token, Formatting.Indented);
				File.WriteAllText(filename, json);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Error saving {0}", filename);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private OAuthToken LoadToken()
		{
			var filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), TOKEN_FILENAME);
			OAuthToken token = null;

			try
			{
				if (File.Exists(filename))
				{
					var json = File.ReadAllText(filename);
					token = JsonConvert.DeserializeObject<OAuthToken>(json);
					_logger.LogInformation("Loaded token from {0}", filename);
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Error loading {0}", filename);
			}
			return token;
		}
	}
}
