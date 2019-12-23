using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Fritz.Twitch.PubSub
{

	/// <summary>
	/// Manage interactions with the Twitch pubsub API
	/// </summary>
	public class Proxy : IDisposable
	{

		private ClientWebSocket _Socket;
		private System.Timers.Timer _PingTimer;
		private System.Timers.Timer _PongTimer;
		private ConfigurationSettings _Configuration;
		private ILogger _Logger;
		private static bool _Reconnect;

		public Proxy(IOptions<ConfigurationSettings> settings, ILoggerFactory loggerFactory)
		{

			InitializeMethodStrategies();

			_Configuration = settings.Value;
			_Logger = loggerFactory.CreateLogger("TwitchPubSub");

		}

		public async Task StartAsync(IEnumerable<TwitchTopic> topics, CancellationToken token)
		{

			_Topics = topics;

			// Start a timer to manage the connection over the websocket
			_PingTimer = new System.Timers.Timer(TimeSpan.FromSeconds(30).TotalMilliseconds);
			_PingTimer.Elapsed += _PingTimer_Elapsed;
			_PingTimer.Start();

			await StartListening(topics);

			while (!token.IsCancellationRequested)
			{

				var buffer = new byte[1024];
				var messageBuffer = new ArraySegment<byte>(buffer);
				var completeMessage = new StringBuilder();

				var result = await _Socket.ReceiveAsync(messageBuffer, token);
				completeMessage.Append(Encoding.UTF8.GetString(messageBuffer));
				while (!result.EndOfMessage)
				{
					buffer = new byte[1024];
					messageBuffer = new ArraySegment<byte>(buffer);
					result = await _Socket.ReceiveAsync(messageBuffer, token);
					completeMessage.Append(Encoding.UTF8.GetString(messageBuffer));
				}

				if (result.MessageType == WebSocketMessageType.Close) {
					_Reconnect = true;
					break;
				}

				try
				{
					HandleMessage(completeMessage.ToString());
				} catch (UnhandledPubSubMessageException) {
					// do nothing
				} catch (Exception e) {
					_Logger.LogError(e, "Error while parsing message from Twitch: " + completeMessage.ToString());
					_Logger.LogError("Reconnecting...");
					_Reconnect = true;
				}

				if (_Reconnect) {
					break;
				}

			}

			if (_Reconnect) _ = Task.Run(() => StartAsync(topics, token));

		}


		private delegate bool OnReceivedMessage(string message);
		private List<OnReceivedMessage> _Strategies = new List<OnReceivedMessage>();

		private void HandleMessage(string receivedMessage)
		{

			var jDoc = JObject.Parse(receivedMessage);
			var messageType = jDoc["type"].Value<string>();
			if (messageType == "RESPONSE" && jDoc["error"].Value<string>() != "")
			{
				throw new Exception("Unable to connect");
			} else if (messageType == "RESPONSE") {
				return;
			}

			foreach (var strategy in _Strategies)
			{
				if (strategy(receivedMessage)) return;
			}

			throw new UnhandledPubSubMessageException();

		}

		private async Task StartListening(IEnumerable<TwitchTopic> topics)
		{

			_Socket = new ClientWebSocket();

			var message = new PubSubListen
			{
				data = new PubSubListen.PubSubListenData
				{
					auth_token = _Configuration.OAuthToken,
					topics = topics.Select(t => t.TopicString).ToArray()
				}
			};

			await _Socket.ConnectAsync(new Uri("wss://pubsub-edge.twitch.tv:443"), CancellationToken.None)
				.ContinueWith(t => SendMessageOnSocket(JsonConvert.SerializeObject(message)));

		}

		private void _PingTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			var message = @"{ ""type"": ""PING"" }";
			SendMessageOnSocket(message).GetAwaiter().GetResult();
			_PongTimer = new System.Timers.Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
			_PongTimer.Elapsed += _PongTimer_Elapsed;
			_PongTimer.Start();
			_PingAcknowledged = false;

			// TODO: handle the lack of returned PONG message 

		}

		private void _PongTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (!_PingAcknowledged) {
				_Reconnect = true;
				_PongTimer.Dispose();
			}
		}

		private Task SendMessageOnSocket(string message)
		{

			if (_Socket.State != WebSocketState.Open) return Task.CompletedTask;

			var byteArray = Encoding.ASCII.GetBytes(message);
			return _Socket.SendAsync(byteArray, WebSocketMessageType.Text, true, CancellationToken.None);

		}

		#region PubSubEvents

		public event EventHandler<ChannelRedemption> OnChannelPointsRedeemed;

		#endregion

		#region Message Handler Strategies

		private void InitializeMethodStrategies() {

			_Strategies.Add(new OnReceivedMessage(HandlePongMessage));
			_Strategies.Add(new OnReceivedMessage(HandleReconnectMessage));
			_Strategies.Add(new OnReceivedMessage(HandleChannelPointsMessage));

		}

		private bool HandlePongMessage(string message) {

			if (message.Contains(@"""PONG""")) {
				_PingAcknowledged = true;
				_PongTimer.Stop();
				_PongTimer.Dispose();
				return true;
			}

			return false;

		}

		private bool HandleReconnectMessage(string message)  {

			if (message.Contains(@"""RECONNECT""")) {

				_Reconnect = true;

				return true;
			}

			return false;

		}

		private bool HandleChannelPointsMessage(string message) {

			var jDoc = JObject.Parse(message);

			if (jDoc["type"].Value<string>() == "MESSAGE" && jDoc["data"]["topic"].Value<string>().StartsWith("channel-points-channel-v1") ) {

				var innerMessage = jDoc["data"]["message"].Value<string>();

				PubSubRedemptionMessage messageObj = null;
				try
				{
					messageObj = JsonConvert.DeserializeObject<PubSubRedemptionMessage>(innerMessage);
				} catch (Exception e) {
					_Logger.LogError(e, "Error while deserializing the message");
					_Logger.LogInformation("Message contents: " + innerMessage);
				}
				OnChannelPointsRedeemed?.Invoke(null, messageObj?.data);
				return true;

			}

			return false;

		}

		#endregion

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls
		private bool _PingAcknowledged;
		private IEnumerable<TwitchTopic> _Topics;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_PingTimer.Dispose();
					_PongTimer.Dispose();
				}

				_Socket.Dispose();

				disposedValue = true;
			}
		}

		~Proxy()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion

	}

}
