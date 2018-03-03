using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Helpers;
using Fritz.StreamTools.Services.Mixer;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace Test.Services.Mixer
{
	public class SimulatedClientWebSocket : IClientWebSocketProxy
	{
		public WebSocketCloseStatus? CloseStatus { get; internal set; }
		public bool IsChat { get; }
		public ManualResetEventSlim JoinedChat { get; } = new ManualResetEventSlim();
		public ManualResetEventSlim JoinedConstallation { get; } = new ManualResetEventSlim();
		public JToken LastPacket { get; private set; }
		public int? LastId { get; private set; }
		public ITestOutputHelper Output { get; set; }

		readonly ManualResetEventSlim _signal = new ManualResetEventSlim();
		readonly ManualResetEventSlim _readEntered = new ManualResetEventSlim();
		readonly ConcurrentQueue<string> _data = new ConcurrentQueue<string>();
		readonly string _welcomeMessage;
		bool _isFirstSend = true;
		private readonly bool _isAuthenticated;
		object _syncObject = new object();

		public SimulatedClientWebSocket(bool isChat, bool isAuthenticated, string welcomeMessage = null)
		{
			IsChat = isChat;
			_welcomeMessage = welcomeMessage;
			_isAuthenticated = isAuthenticated;
		}

		virtual public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
		{
			CloseStatus = null;

			// Enqueue welcome messages
			if (!string.IsNullOrEmpty(_welcomeMessage))
			{
				_data.Enqueue(_welcomeMessage);
				_signal.Set();
			}

			Output.WriteLine($"{GetHashCode():X8} CONNECT {uri}");
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			Output.WriteLine($"{GetHashCode()} Disposing!");
			lock (_syncObject)
			{
				CloseStatus = WebSocketCloseStatus.NormalClosure;
				_data.Clear();
				_signal.Set();
			}
		}

		virtual public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
		{
			if (CloseStatus.HasValue)
			{
				throw new WebSocketException("Simulated WebSocket closed");
			}

			_readEntered.Set();

			string json = null;
			while(true)
			{
				lock (_syncObject)
				{
					if (!_data.TryDequeue(out json))
					{
						_signal.Reset();
						if (CloseStatus.HasValue)
						{
							throw new WebSocketException("Simulated WebSocket closed");
						}
					}
					else
						break;
				}
				var timeout = ( Debugger.IsAttached ) ? Timeout.Infinite : Simulator.TIMEOUT;
				_signal.Wait(timeout);
				if (CloseStatus.HasValue)
				{
					throw new WebSocketException("Simulated WebSocket closed");
				}
			}

			var bytes = Encoding.UTF8.GetBytes(json);
			bytes.CopyTo(buffer.Array, buffer.Offset);
			return Task.FromResult(new WebSocketReceiveResult(bytes.Length, WebSocketMessageType.Text, true));
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
					JoinedChat.Set();
					if(_isAuthenticated)
					{
						InjectPacket("{'type':'reply','error':null,'id':<MSGID>,'data':{'authenticated':true,'roles':[]}}"
							.Replace("'", "\"")
							.Replace("<MSGID>", LastId.ToString())
						);
					}
					else
					{
						InjectPacket("{'type':'reply','error':null,'id':<MSGID>,'data':null}"
							.Replace("'", "\"")
							.Replace("<MSGID>", LastId.ToString())
						);
					}
				}
				else
				{
					JoinedConstallation.Set();
					if (_isAuthenticated)
						InjectPacket("{'type':'reply','error':null,'id':<MSGID>,'data':{'authenticated':true,'roles':['Owner','User']}}".Replace("'", "\"").Replace("<MSGID>", LastId.ToString()));
					else
						InjectPacket("{'id':<MSGID>,'type':'reply','result':null,'error':null}".Replace("'", "\"").Replace("<MSGID>", LastId.ToString()));
				}
			}

			return Task.CompletedTask;
		}

		virtual public void SetRequestHeader(string name, string value) { }

		public void InjectPacket(string json)
		{
			var timeout = Debugger.IsAttached ? Timeout.Infinite : Simulator.TIMEOUT;
			lock (_syncObject)
			{
				_readEntered.Reset();

				// Enqueue data for the receiver
				_data.Enqueue(json);
				_signal.Set();
			}

			// Wait until client code has processed the message (its back waiting for more)
			_readEntered.Wait(timeout);
		}
	}
}
