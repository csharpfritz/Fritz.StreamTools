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
		private readonly System.Timers.Timer _PingTimer;
		private ConfigurationSettings _Configuration;
		private ILogger _Logger;
		private static bool _Reconnect;

		public Proxy(IOptions<ConfigurationSettings> settings, ILoggerFactory loggerFactory)
		{

			InitializeMethodStrategies();

			_Configuration = settings.Value;
			_Logger = loggerFactory.CreateLogger("TwitchPubSub");

			_Socket = new ClientWebSocket();

			// Start a timer to manage the connection over the websocket
			_PingTimer = new System.Timers.Timer(TimeSpan.FromMinutes(4).TotalMilliseconds);
			_PingTimer.Elapsed += _PingTimer_Elapsed;
			_PingTimer.Start();

		}

		public async Task StartAsync(IEnumerable<TwitchTopic> topics, CancellationToken token)
		{

			_Topics = topics;
			await StartListening(topics);
			var messageBuffer = new Memory<byte>();


			while (!token.IsCancellationRequested)
			{

				await _Socket.ReceiveAsync(messageBuffer, token);

				HandleMessage(UTF8Encoding.UTF8.GetString(messageBuffer.Span));

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

			foreach (var strategy in _Strategies)
			{
				if (strategy(receivedMessage)) return;
			}

			throw new UnhandledPubSubMessageException();

		}

		private async Task StartListening(IEnumerable<TwitchTopic> topics)
		{

			await _Socket.ConnectAsync(new Uri("wss://pubsub-edge.twitch.tv"), CancellationToken.None);

			var message = new PubSubListen
			{
				data = new PubSubListen.PubSubListenData
				{
					auth_token = _Configuration.OAuthToken,
					topics = topics.Select(t => t.TopicString).ToArray()
				}
			};

			await SendMessageOnSocket(JsonConvert.SerializeObject(message));

		}

		private void _PingTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			var message = @"{ ""type"": ""PING"" }";
			SendMessageOnSocket(message).GetAwaiter().GetResult();
			_PingAcknowledged = false;

			// TODO: handle the lack of returned PONG message 

		}

		private async Task SendMessageOnSocket(string message)
		{

			var byteArray = Encoding.UTF8.GetBytes(message);
			await _Socket.SendAsync(byteArray, WebSocketMessageType.Text, false, CancellationToken.None);

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

			if (jDoc["type"].Value<string>() == "reward-redeemed") {

				var messageObj = JsonConvert.DeserializeObject<PubSubMessage<ChannelRedemption>>(message);

				OnChannelPointsRedeemed?.BeginInvoke(null, messageObj.data, null, null);
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
