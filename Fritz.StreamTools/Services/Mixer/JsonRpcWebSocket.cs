using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Fritz.StreamTools.Services.Mixer
{
	public class EventEventArgs : EventArgs
	{
		public string Event { get; set; }
		public JToken Data { get; set; }
	}

	public class JsonRpcWebSocket : IDisposable
  {
		ClientWebSocket _ws;
		CancellationTokenSource _cancellationToken = new CancellationTokenSource();
		ILogger _logger;
		Task _task;
		bool _disposed;
		int _nextId = 0;
		ConcurrentDictionary<int, TaskCompletionSource<bool>> _pendingRequests = new ConcurrentDictionary<int, TaskCompletionSource<bool>>();
		readonly bool _isChat;

		/// <summary>
		/// Raised each time an event is received on the websocket
		/// </summary>
		public event EventHandler<EventEventArgs> OnEventReceived;

		/// <summary>
		/// Construct a new JsonRpcWebSocket object
		/// </summary>
		public JsonRpcWebSocket(ILoggerFactory loggerFactory, bool isChat)
		{
			_logger = loggerFactory.CreateLogger<JsonRpcWebSocket>();
			_isChat = isChat;
		}

		/// <summary>
		/// Try connecting to the specified websocker url
		/// </summary>
		public async Task<bool> TryConnectAsync(string url, string accessToken = null)
		{
			if (_disposed) throw new ObjectDisposedException(nameof(JsonRpcWebSocket));

			_ws = new ClientWebSocket();
			_ws.Options.SetRequestHeader("x-is-bot", "true");
			if (!string.IsNullOrEmpty(accessToken))
			{
				_ws.Options.SetRequestHeader("Authorization", $"Bearer {accessToken}");
			}

			try
			{
				// Connect the websocket
				await _ws.ConnectAsync(new Uri(url), new CancellationTokenSource(10000).Token);

				// Start task for received data
				_task = Task.Run(async () =>
				{
					var segment = new ArraySegment<byte>(new byte[4096]);

					while (!_cancellationToken.IsCancellationRequested)
					{
						// Get next packet (will block)
						// NOTE: I expect to receive a complete packet, which might not be correct ?!?
						var result = await _ws.ReceiveAsync(segment, CancellationToken.None);
						if (result == null || result.Count == 0)
						{
							break;  // Websocket closed
						}

						// Wait for the next packet from the server
						var json = Encoding.UTF8.GetString(segment.Array.Take(result.Count).ToArray());
						var doc = JToken.Parse(json);
						switch(doc["type"].Value<string>())
						{
							case "reply":
								// We received a reply to a command

								var id = doc["id"].Value<int>();
								var error = doc["error"];
								if(error.HasValues)
								{
									_logger.LogError($"Code: {(int)error["code"]} Message: '{(string)error["message"]}'");
								}

								if (_pendingRequests.TryGetValue(id, out var task))
								{
									if (error.HasValues)
										task.SetResult(true);
									else
										task.SetResult(false);
								}
								break;
							case "event":
								// Some event received, chat message maybe ?
								OnEventReceived?.Invoke(this, new EventEventArgs { Event = doc["event"].Value<string>(), Data = doc["data"] });
								break;
						}
					}
				});

				return true;
			}
			catch (Exception e)
			{
				_logger.LogError("Connection to mixer chat failed: {0}", e.Message);
				return false;
			}
		}

		/// <summary>
		/// Send a command to the server, will wait for reply
		/// </summary>
		/// <returns>true if success, or false if error</returns>
		public async Task<bool> SendAsync(string method, params object[] args)
		{
			if (_disposed) throw new ObjectDisposedException(nameof(JsonRpcWebSocket));

			var id = _nextId++;
			var doc = new JObject
			{
				{ "id", id },
				{ "type", "method" },
				{ "method", method }
			};

			if (_isChat)
			{
				if (args != null && args.Length != 0) doc.Add(new JProperty("arguments", args));
			}
			else
			{
				doc.Add("params", JObject.FromObject(new { events = args }));
			}

			var json = doc.ToString(Newtonsoft.Json.Formatting.None);
			var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));

			// Send request and wait for reply (or timeout)
			var tcs = new TaskCompletionSource<bool>();
			_pendingRequests.TryAdd(id, tcs);
		
			try
			{
				await _ws.SendAsync(buffer, WebSocketMessageType.Text, true, _cancellationToken.Token);
				await tcs.Task.OrTimeout();
				return tcs.Task.Result;
			}
			finally
			{
				_pendingRequests.TryRemove(id, out var _);
			}
		}

		/// <summary>
		/// Closes the websocket connection and stops the receiving task
		/// </summary>
		public void Dispose()
		{
			if (_disposed) throw new ObjectDisposedException(nameof(JsonRpcWebSocket));

			if(_ws != null)
			{
				_cancellationToken.Cancel();
				_ws.Dispose();

				if (_task != null)
				{
					_task.Wait();
				}
			}
			_disposed = true;
		}
	}
}
