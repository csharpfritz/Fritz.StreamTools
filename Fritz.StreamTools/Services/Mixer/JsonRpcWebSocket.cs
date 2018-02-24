using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
		const int CONNECT_TIMEOUT = 20000;	// In milliseconds
		ClientWebSocket _ws;
		CancellationTokenSource _cancellationToken = new CancellationTokenSource();
		ILogger _logger;
		bool _disposed;
		int _nextPacketId = 0;
		ConcurrentDictionary<int, TaskCompletionSource<bool>> _pendingRequests = new ConcurrentDictionary<int, TaskCompletionSource<bool>>();
		readonly bool _isChat;
		ConcurrentQueue<string> _myMessages = new ConcurrentQueue<string>();
		Task _receiverTask;
		int? _receiverThreadId;

		/// <summary>
		/// Raised each time an event is received on the websocket
		/// </summary>
		public event EventHandler<EventEventArgs> EventReceived;

		/// <summary>
		/// Construct a new JsonRpcWebSocket object
		/// </summary>
		public JsonRpcWebSocket(ILogger logger, bool isChat)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_isChat = isChat;
		}

		/// <summary>
		/// Try connecting to the specified websocker url
		/// </summary>
		public async Task<bool> TryConnectAsync(Func<string> resolveUrl, string accessToken, Func<Task> connectCompleted)
		{
			if (resolveUrl == null)
				throw new ArgumentNullException(nameof(resolveUrl));
			if (_disposed) throw new ObjectDisposedException(nameof(JsonRpcWebSocket));

			// Local function that will try to reconnect forever
			async Task reconnect()
			{
				while (true)
				{
					await connect();
					if (_ws != null) return;

					// Connect failed, wait a little and try again
					await Task.Delay(5000, _cancellationToken.Token);
				}
			}

			// Local function used for connect and re-connects
			async Task connect()
			{
				_ws = null;
				var url = resolveUrl();

				try
				{
					var ws = new ClientWebSocket();
					ws.Options.SetRequestHeader("x-is-bot", "true");
					if (!string.IsNullOrEmpty(accessToken))
					{
						ws.Options.SetRequestHeader("Authorization", $"Bearer {accessToken}");
					}

					// Connect the websocket
					_logger.LogInformation("Connecting to {0}", url);
					await ws.ConnectAsync(new Uri(url), _cancellationToken.Token).OrTimeout(CONNECT_TIMEOUT);
					_logger.LogInformation("Connected to {0}", url);
					_ws = ws;

					await EatWelcomeMessage().OrTimeout(10000);

					// start receiving data
					_receiverTask = Task.Factory.StartNew(() => ReceiverTask(reconnect), TaskCreationOptions.LongRunning);

					if (connectCompleted != null) await connectCompleted();
				}
				catch (Exception e)
				{
					_ws = null;
					_logger.LogWarning("Connection to '{0}' failed: {1}", url, e.Message);
				}
			}

			await connect();
			return _ws != null;
		}

		private async Task EatWelcomeMessage()
		{
			var segment = new ArraySegment<byte>(new byte[100]);
			var result = await _ws.ReceiveAsync(segment, CancellationToken.None);
			var json = Encoding.UTF8.GetString(segment.Array.Take(result.Count).ToArray());
			_logger.LogTrace("<< " + json);
		}

		/// <summary>
		/// Received data from the websocket.
		/// This will run for the lifetime of the connection or until cancellation is requested
		/// </summary>
		private async Task ReceiverTask(Func<Task> reconnect)
		{
			_receiverThreadId = Thread.CurrentThread.ManagedThreadId;
			var segment = new ArraySegment<byte>(new byte[4096]);
			var ws = _ws;

			while (!_cancellationToken.IsCancellationRequested)
			{
				JToken doc;
				try
				{
					// Get next packet (will block)
					// NOTE: I expect to receive a complete packet, which might not be correct ?!?
					var result = await ws.ReceiveAsync(segment, _cancellationToken.Token);
					if (result == null || result.Count == 0) return;

					var json = Encoding.UTF8.GetString(segment.Array.Take(result.Count).ToArray());
					_logger.LogTrace("<< " + json);
					doc = JToken.Parse(json);


					switch (doc["type"].Value<string>())
					{
						case "reply":
							HandleReply(doc);
							break;
						case "event":
							HandleEvent(doc);
							break;
					}
				}
				catch (Exception e)
				{
					_logger.LogWarning("Error in ReceiverTask() {0}. Will reconnect", e.Message);
					if (_cancellationToken.IsCancellationRequested) return;

					await reconnect();  // Will spawn a new receiver task
					return;
				}
			}
		}

		private void HandleEvent(JToken doc)
		{
			// Ignore messages I have send
			var msgId = (string)doc["data"]["id"];
			if (_myMessages.Contains(msgId)) return;

			// Some event received, chat message maybe ?
			EventReceived?.Invoke(this, new EventEventArgs { Event = doc["event"].Value<string>(), Data = doc["data"] });
		}

		private void HandleReply(JToken doc)
		{
			var error = doc["error"];
			if (error.HasValues)
			{
				_logger.LogError($"Error from server: Code = {(int)error["code"]} Message = '{(string)error["message"]}'");
			}

			if (doc["data"] != null && doc["data"]["id"] != null)
			{
				// Remember last 5 messages I have send
				_myMessages.Enqueue((string)doc["data"]["id"]);
				while (_myMessages.Count > 5) _myMessages.TryDequeue(out var _);
			}

			var id = doc["id"].Value<int>();
			if (_pendingRequests.TryGetValue(id, out var task))
			{
				// Signal waiting task that we have received a reply
				if (error.HasValues)
					task.SetResult(false);
				else
					task.SetResult(true);
			}
		}

		/// <summary>
		/// Send a command to the server, will wait for reply
		/// </summary>
		/// <returns>true if success, or false if error</returns>
		public async Task<bool> SendAsync(string method, params object[] args)
		{
			if (_disposed) throw new ObjectDisposedException(nameof(JsonRpcWebSocket));
			if (Thread.CurrentThread.ManagedThreadId == _receiverThreadId)
				throw new Exception("Cannot call SendAsync on same thread as websocket receiver thread!");

			var ws = _ws;
			if (ws == null)
				return false;

			var id = Interlocked.Increment(ref _nextPacketId);
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

			if (_logger.IsEnabled(LogLevel.Trace))
			{
				if (method == "auth")
				{
					// hide the authKey from log
					Debug.Assert(args.Length >= 3);
					_logger.LogTrace(">> " + json.Replace((string)args[2], "(chatAuthKey)"));
				}
				else
				{
					_logger.LogTrace(">> " + json);
				}
			}

			var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));

			// Send request and wait for reply (or timeout)
			var tcs = new TaskCompletionSource<bool>();
			_pendingRequests.TryAdd(id, tcs);
		
			try
			{
				await ws.SendAsync(buffer, WebSocketMessageType.Text, true, _cancellationToken.Token);
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

			_cancellationToken.Cancel();

			if (_ws != null)
			{
				_ws.Dispose();
				if (_receiverTask != null) _receiverTask.Wait();
			}
			_disposed = true;
		}
	}
}
