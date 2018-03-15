using System.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

// Talks to the OBS plugin 'obs-websocket' to remotely control the volume of a single Audio Device
// Get the plugin here: https://github.com/Palakis/obs-websocket

namespace Fritz.StreamTools.Services
{
	public interface IOBSRemoteControl
	{
		/// <summary>
		/// Set the volume of the OBS audio device
		/// </summary>
		/// <param name="volume">A value between 0 and 1</param>
		Task SetVolumeAsync(float volume);

		/// <summary>
		/// Get the volume of the OBS audio device
		/// </summary>
		/// <returns>A value between 0 and 1</returns>
		Task<float> GetVolumeAsync();
	}

	internal class OBSRemoteControl : IOBSRemoteControl
	{
		ClientWebSocket _webSocket;
		Task _receiverTask;
		readonly IConfiguration _config;
		readonly ILogger _logger;
		readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
		CancellationTokenSource _shutdown;
		readonly ConcurrentDictionary<int, TaskCompletionSource<JObject>> _pendingRequests = new ConcurrentDictionary<int, TaskCompletionSource<JObject>>();
		readonly byte[] _buffer = new byte[1024];
		int _nextId = 1;
		string _audioDevice;

		public OBSRemoteControl(ILoggerFactory loggerFactory, IConfiguration config)
		{
			_config = config;
			_logger = loggerFactory.CreateLogger(nameof(OBSRemoteControl));
		}

		private async Task<bool> EnsureConnected()
		{
			string url = null;

			if (_webSocket != null)
				return true;

			_shutdown = new CancellationTokenSource();

			url = _config["OBSRemoteControl:Url"];
			if (string.IsNullOrEmpty(url))
			{
				_logger.LogWarning("No OBSRemoteControl:Url config parameter");
				return false;
			}

			_audioDevice = _config["OBSRemoteControl:AudioDevice"];
			if (string.IsNullOrEmpty(_audioDevice))
			{
				_logger.LogWarning("No OBSRemoteControl:AudioDevice config parameter");
				return false;
			}

			async Task connect()
			{
				try
				{
					_lock.Wait();

					var ws = new ClientWebSocket();
					ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

					await ws.ConnectAsync(new Uri(url), _shutdown.Token);
					_webSocket = ws;
					_receiverTask = Task.Factory.StartNew(() => ReceiverTask(connect), TaskCreationOptions.LongRunning);
					_logger.LogInformation("Connected to {0}", url);

					var authInfo = await GetAuthInfoAsync();
					if (authInfo.AuthRequired)
					{
						var password = _config["OBSRemoteControl:Password"];
						if (string.IsNullOrEmpty(password))
							throw new Exception("Password needed for authentication");

						if (!await AuthenticateAsync(password, authInfo))
						{
							_logger.LogError("Authentication failed!");
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError("Failed to connect to OBS at '{0}': {1}", url, ex.Message);
					_shutdown.Cancel();
					_webSocket?.Dispose();
					_webSocket = null;
				}
				finally
				{
					_lock.Release();
				}
			}

			await connect();
			return true;
		}

		private async Task<bool> AuthenticateAsync(string password, OBSAuthInfo authInfo)
		{
			var secret = HashEncode(password + authInfo.PasswordSalt);
			var authResponse = HashEncode(secret + authInfo.Challenge);

			var requestFields = new JObject { { "auth", authResponse } };

			var reply = await SendAsync("Authenticate", requestFields);
			return reply["status"]?.Value<string>() == "ok";
		}

		private async Task<JObject> SendAsync(string requestType, JObject additionalFields = null)
		{
			var id = Interlocked.Increment(ref _nextId);

			// Build the bare-minimum body for a request
			var body = new JObject {
				{ "request-type", requestType },
				{ "message-id", id.ToString() }
			};

			// Add optional fields if provided
			if (additionalFields != null)
				body.Merge(additionalFields);

			// Prepare the asynchronous response handler
			var tcs = new TaskCompletionSource<JObject>();
			_pendingRequests.TryAdd(id, tcs);

			try
			{
				var s = body.ToString(Formatting.None);
				_logger.LogTrace(">> {0}", s);
				var data = Encoding.UTF8.GetBytes(s);
				await _webSocket.SendAsync(data, WebSocketMessageType.Text, true, _shutdown.Token);
				await tcs.Task;
			}
			finally
			{
				_pendingRequests.Remove(id, out var _);
			}

			var result = tcs.Task.Result;
			if (result["status"]?.Value<string>() == "error")
				throw new Exception((string)result["error"]);

			return result;
		}

		private async Task ReceiverTask(Func<Task> reconnect)
		{
			while (true)
			{
				var result = await _webSocket.ReceiveAsync(_buffer, _shutdown.Token);
				if (result == null || result.CloseStatus != null || result.Count == 0 || _shutdown.IsCancellationRequested)
					break;

				Debug.Assert(result.EndOfMessage);
				var s = Encoding.UTF8.GetString(_buffer, 0, result.Count);
				var body = JObject.Parse(s);
				_logger.LogTrace("<< {0}", body.ToString(Formatting.None));

				if (body["message-id"] != null)
				{
					var id = (string)body["message-id"];
					if (id != null)
					{
						_pendingRequests[int.Parse(id)]?.SetResult(body);
					}
				}
				else if (body["update-type"] != null)
				{
					// NOP
				}
			}

			_lock.Wait();
			_webSocket.Dispose();
			_webSocket = null;
			_lock.Release();

			if (!_shutdown.IsCancellationRequested)
				await reconnect();
		}

		private string HashEncode(string input)
		{
			var sha256 = new SHA256Managed();
			var textBytes = Encoding.ASCII.GetBytes(input);
			var hash = sha256.ComputeHash(textBytes);
			return Convert.ToBase64String(hash);
		}

		public async Task<OBSAuthInfo> GetAuthInfoAsync()
		{
			var response = await SendAsync("GetAuthRequired");
			return new OBSAuthInfo(response);
		}

		public async Task<float> GetVolumeAsync()
		{
			if (!await EnsureConnected())
				return 0;

			var requestFields = new JObject();
			requestFields.Add("source", _audioDevice);

			var response = await SendAsync("GetVolume", requestFields);
			var vi = new VolumeInfo(response);
			return vi.Volume;
		}

		public async Task SetVolumeAsync(float volume)
		{
			if (volume < 0 || volume > 1)
				throw new ArgumentOutOfRangeException(nameof(volume));
			if (!await EnsureConnected())
				return;

			var requestFields = new JObject();
			requestFields.Add("source", _audioDevice);
			requestFields.Add("volume", volume);

			await SendAsync("SetVolume", requestFields);
		}

		/// <summary>
		/// Data required by authentication
		/// </summary>
		public struct OBSAuthInfo
		{
			/// <summary>
			/// True if authentication is required, false otherwise
			/// </summary>
			public readonly bool AuthRequired;

			/// <summary>
			/// Authentication challenge
			/// </summary>
			public readonly string Challenge;

			/// <summary>
			/// Password salt
			/// </summary>
			public readonly string PasswordSalt;

			/// <summary>
			/// Builds the object from JSON response body
			/// </summary>
			/// <param name="data">JSON response body as a <see cref="JObject"/></param>
			public OBSAuthInfo(JObject data)
			{
				AuthRequired = (bool)data["authRequired"];
				Challenge = (string)data["challenge"];
				PasswordSalt = (string)data["salt"];
			}
		}

		/// <summary>
		/// Volume settings of an OBS source
		/// </summary>
		public struct VolumeInfo
		{
			/// <summary>
			/// Source volume in linear scale (0.0 to 1.0)
			/// </summary>
			public readonly float Volume;

			/// <summary>
			/// True if source is muted, false otherwise
			/// </summary>
			public readonly bool Muted;

			/// <summary>
			/// Builds the object from the JSON response body
			/// </summary>
			/// <param name="data">JSON response body as a <see cref="JObject"/></param>
			public VolumeInfo(JObject data)
			{
				Volume = (float)data["volume"];
				Muted = (bool)data["muted"];
			}
		}
	}
}
