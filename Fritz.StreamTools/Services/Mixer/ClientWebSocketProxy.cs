using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IClientWebSocketProxy : IDisposable
	{
		bool IsChat { get; }
		WebSocketCloseStatus? CloseStatus { get; }
		void SetRequestHeader(string name, string value);
		Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
		Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);
		Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
	}

	/// <summary>
	/// Encapsulates a real ClientWebSocket so it can be accessed using a interface
	/// </summary>
	public class ClientWebSocketProxy : IClientWebSocketProxy
	{
		private readonly ClientWebSocket _ws;

		public ClientWebSocketProxy(bool isChat)
		{
			IsChat = isChat;
			_ws = new ClientWebSocket();
			_ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
		}

		public bool IsChat { get; }
		public WebSocketCloseStatus? CloseStatus { get => _ws.CloseStatus; }
		public void SetRequestHeader(string name, string value) => _ws.Options.SetRequestHeader(name, value);
		public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) => _ws.ConnectAsync(uri, cancellationToken);
		public void Dispose() => _ws.Dispose();
		public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) => _ws.ReceiveAsync(buffer, cancellationToken);
		public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
			=> _ws.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
	}
}
