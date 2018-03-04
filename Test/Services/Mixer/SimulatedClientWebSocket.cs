using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Services.Mixer;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace Test.Services.Mixer
{
	public class SimulatedClientWebSocket : IClientWebSocketProxy
	{
		static int _connectionId = 0;

		public WebSocketCloseStatus? CloseStatus { get; internal set; }
		public bool IsChat { get; }
		public ManualResetEventSlim JoinedChat { get; } = new ManualResetEventSlim();
		public ManualResetEventSlim JoinedConstallation { get; } = new ManualResetEventSlim();
		public JToken LastPacket { get; private set; }
		public int? LastId { get; private set; }

		public string ConnectUrl { get; set; }
		public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

		readonly ManualResetEventSlim _clientDoneProcessing = new ManualResetEventSlim();
		readonly string _welcomeMessage;
		bool _isFirstSend = true;
		private readonly bool _isAuthenticated;
		NamedPipeServerStream _serverPipe;
		NamedPipeClientStream _injectPipe;
		int _myId;

		public SimulatedClientWebSocket(bool isChat, bool isAuthenticated, string welcomeMessage = null)
		{
			IsChat = isChat;
			_welcomeMessage = welcomeMessage;
			_isAuthenticated = isAuthenticated;

			_myId = Interlocked.Increment(ref _connectionId);

			string pipeName = "FritzTestPipe_" + GetHashCode().ToString();
			_serverPipe = new NamedPipeServerStream(pipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
			var t = _serverPipe.WaitForConnectionAsync();
			_injectPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
			_injectPipe.Connect();
			t.Wait();
		}

		virtual public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
		{
			CloseStatus = null;
			ConnectUrl = uri.ToString();

			// Enqueue welcome messages
			if (!string.IsNullOrEmpty(_welcomeMessage))
			{
				var bytes = Encoding.UTF8.GetBytes(_welcomeMessage);
				_injectPipe.WriteAsync(bytes, 0, bytes.Length);
			}

			Log($"SimWebSocket CONNECTED {uri}");
			return Task.CompletedTask;
		}

		virtual public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
		{
			if (CloseStatus.HasValue)
				throw new WebSocketException("WebSocket is closed");

			int n = await _serverPipe.ReadAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken);
			if (cancellationToken.IsCancellationRequested)
				return null;
			if(n == 0)
				throw new WebSocketException("WebSocket closed");

			return new WebSocketReceiveResult(n, WebSocketMessageType.Text, true);
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

		virtual public void SetRequestHeader(string name, string value)
		{
			Headers.Remove(name);	// Remove if its already there
			Headers.Add(name, value);
		}

		public void InjectPacket(string json)
		{
			if (!_injectPipe.IsConnected)
			{
				Log("Cant inject packet, not connected!");
				return;
			}

			_clientDoneProcessing.Reset();

			var bytes = Encoding.UTF8.GetBytes(json);
			_injectPipe.Write(bytes, 0, bytes.Length);
			_injectPipe.WaitForPipeDrain();

			// Wait until client code has processed the message (its back waiting for more)
			var timeout = Debugger.IsAttached ? Timeout.Infinite : Simulator.TIMEOUT;
			_clientDoneProcessing.Wait(timeout);
		}

		private void Log(string format, params object[] args)
		{
			// Output?.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.ffff")} - {string.Format(format, args)}");
		}

		public void ProcessingDone() => _clientDoneProcessing.Set();

		public void Dispose()
		{
			Log("SimWebSocket Disposing!");
			CloseStatus = WebSocketCloseStatus.NormalClosure;
			_injectPipe.Dispose();
			_serverPipe.Dispose();
		}
	}
}
