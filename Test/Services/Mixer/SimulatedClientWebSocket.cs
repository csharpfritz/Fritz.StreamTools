using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Helpers;
using Fritz.StreamTools.Services.Mixer;
using Newtonsoft.Json.Linq;

namespace Test.Services.Mixer
{
	public class SimulatedClientWebSocket : IClientWebSocketProxy
	{
		public WebSocketCloseStatus? CloseStatus { get; internal set; }
		public bool IsChat { get; }
		public bool JoinedChat { get; internal set; }
		public bool JoinedConstallation { get; internal set; }
		public JToken LastPacket { get; private set; }
		public int? LastId { get; private set; }

		readonly AsyncManualResetEvent _signal = new AsyncManualResetEvent();
		readonly AsyncManualResetEvent _readEntered = new AsyncManualResetEvent();
		readonly ConcurrentQueue<string> _data = new ConcurrentQueue<string>();
		readonly string _welcomeMessage;
		bool _isFirstSend = true;

		public SimulatedClientWebSocket(bool isChat, string welcomeMessage = null)
		{
			IsChat = isChat;
			_welcomeMessage = welcomeMessage;
		}

		virtual public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
		{
			CloseStatus = WebSocketCloseStatus.Empty;

			// Enqueue welcome messages
			if (!string.IsNullOrEmpty(_welcomeMessage))
			{
				_data.Enqueue(_welcomeMessage);
				_signal.Set();
			}

			return Task.CompletedTask;
		}

		public void Dispose()
		{
			SimulateDisconnect();
		}

		virtual public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
		{
			_readEntered.Set();

			var tcs = new TaskCompletionSource<int>();

			if (_data.IsEmpty) _signal.Reset();
			await _signal.WaitAsync();

			if(_data.TryDequeue(out var json))
			{
				var bytes = Encoding.UTF8.GetBytes(json);
				bytes.CopyTo(buffer.Array, buffer.Offset);
				return new WebSocketReceiveResult(bytes.Length, WebSocketMessageType.Text, true);
			}
			else
			{
				throw new Exception("Simulated disconnect");
			}
		}

		virtual public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
		{
			var json = Encoding.UTF8.GetString(buffer.Array, 0, buffer.Count);
			LastPacket = JToken.Parse(json);
			LastId = LastPacket["id"]?.Value<int>();

			if (_isFirstSend)
			{
				_isFirstSend = false;
				if (IsChat)
				{
					JoinedChat = true;
					InjectPacket("{'type':'reply','error':null,'id':<MSGID>,'data':{'authenticated':false,'roles':[]}}".Replace("'", "\"").Replace("<MSGID>", LastId.ToString())).Forget();
				}
				else
				{
					JoinedConstallation = true;
					InjectPacket("{'id':<MSGID>,'type':'reply','result':null,'error':null}".Replace("'", "\"").Replace("<MSGID>", LastId.ToString())).Forget();
				}
			}

			return Task.CompletedTask;
		}

		virtual public void SetRequestHeader(string name, string value) { }

		public async Task InjectPacket(string json)
		{
			// 1. Make sure user task is waiting i ReceiveAsync()
			// 2. Enqueue data to be sent
			// 3. Wait for user task to re-enter ReceiveAsync() (its done precessing the message)

			await _readEntered.WaitAsync().OrTimeout(1000);
			_readEntered.Reset();

			_data.Enqueue(json);
			_signal.Set();

			await _readEntered.WaitAsync().OrTimeout(1000);
		}

		public void SimulateDisconnect()
		{
			CloseStatus = WebSocketCloseStatus.NormalClosure;
			_data.Clear();
			_signal.Set();
		}
	}
}
