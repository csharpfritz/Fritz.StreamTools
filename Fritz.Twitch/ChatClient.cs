using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.Twitch
{

	public class ChatClient : IDisposable
	{

		public const string LOGGER_CATEGORY = "Fritz.TwitchChat";
		private TcpClient _TcpClient;
		private StreamReader inputStream;
		private StreamWriter outputStream;
		private int _Retries;
		private Task _ReceiveMassagesTask;
		private MemoryStream _ReceiveStream = new MemoryStream();

		internal static readonly Regex reUserName = new Regex(@"!([^@]+)@");
		internal static readonly Regex reBadges = new Regex(@"badges=([^;]*)");
		internal static Regex reChatMessage;
		internal static Regex reWhisperMessage;

		public event EventHandler<ChatConnectedEventArgs> Connected;
		public event EventHandler<NewMessageEventArgs> NewMessage;
		public event EventHandler<ChatUserJoinedEventArgs> UserJoined;

		private DateTime _NextReset;
		private int _RemainingThrottledCommands;
		// private static readonly ReaderWriterLockSlim _

		public ChatClient(IOptions<ConfigurationSettings> settings, ILoggerFactory loggerFactory) : this(settings.Value, loggerFactory.CreateLogger(LOGGER_CATEGORY))
		{

		}

		internal ChatClient(ConfigurationSettings settings, ILogger logger)
		{

			this.Settings = settings;
			this.Logger = logger;

			reChatMessage = new Regex($@"PRIVMSG #{Settings.ChannelName} :(.*)$");
			reWhisperMessage = new Regex($@"WHISPER {Settings.ChatBotName} :(.*)$");

			_Shutdown = new CancellationTokenSource();

		}

		~ChatClient()
		{

			try {
				Logger?.LogError("GC the ChatClient");
			} catch {}
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		public void Init()
		{

			Connect();

			_ReceiveMessagesThread = new Thread(ReceiveMessagesOnThread);
			_ReceiveMessagesThread.Start();

		}

		public ConfigurationSettings Settings { get; }
		public ILogger Logger { get; }

		public string ChannelName => Settings.ChannelName;

		private readonly CancellationTokenSource _Shutdown;

		private void Connect()
		{

			_TcpClient = new TcpClient("irc.chat.twitch.tv", 80);

			inputStream = new StreamReader(_TcpClient.GetStream());
			outputStream = new StreamWriter(_TcpClient.GetStream());

			Logger.LogTrace("Beginning IRC authentication to Twitch");
			outputStream.WriteLine("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");
			outputStream.WriteLine($"PASS oauth:{Settings.OAuthToken}");
			outputStream.WriteLine($"NICK {Settings.ChatBotName}");
			outputStream.WriteLine($"USER {Settings.ChatBotName} 8 * :{Settings.ChatBotName}");
			outputStream.Flush();

			outputStream.WriteLine($"JOIN #{Settings.ChannelName}");
			outputStream.Flush();

			Connected?.Invoke(this, new ChatConnectedEventArgs());

		}

		private void SendMessage(string message, bool flush = true)
		{

			var throttled = CheckThrottleStatus();

			Thread.Sleep(throttled.GetValueOrDefault(TimeSpan.FromSeconds(0)));

			outputStream.WriteLine(message);
			if (flush)
			{
				outputStream.Flush();
			}

		}

		private TimeSpan? CheckThrottleStatus()
		{

			var throttleDuration = TimeSpan.FromSeconds(30);
			var maximumCommands = 100;

			if (_NextReset == null)
			{
				_NextReset = DateTime.UtcNow.Add(throttleDuration);
			} else if (_NextReset < DateTime.UtcNow)
			{
				_NextReset = DateTime.UtcNow.Add(throttleDuration);
			}

			// TODO: FInish checking and enforcing the chat throttling

			return null;


		}

		/// <summary>
		/// Public async interface to post messages to channel
		/// </summary>
		/// <param name="message"></param>
		public void PostMessage(string message)
		{

			var fullMessage = $":{Settings.ChatBotName}!{Settings.ChatBotName}@{Settings.ChatBotName}.tmi.twitch.tv PRIVMSG #{Settings.ChannelName} :{message}";

			SendMessage(fullMessage);

		}

		public void WhisperMessage(string message, string userName)
		{

			var fullMessage = $":{Settings.ChatBotName}!{Settings.ChatBotName}@{Settings.ChatBotName}.tmi.twitch.tv PRIVMSG #jtv :/w {userName} {message}";
			SendMessage(fullMessage);

		}

		private void ReceiveMessagesOnThread()
		{

			var lastMessageReceivedTimestamp = DateTime.Now;
			var errorPeriod = TimeSpan.FromSeconds(60);

			while (true)
			{

				Thread.Sleep(50);

				if (DateTime.Now.Subtract(lastMessageReceivedTimestamp) > errorPeriod)
				{
					Logger.LogError($"Haven't received a message in {errorPeriod.TotalSeconds} seconds");
					lastMessageReceivedTimestamp = DateTime.Now;
				}

				if (_Shutdown.IsCancellationRequested)
				{
					break;
				}

				if (_TcpClient.Connected && _TcpClient.Available > 0)
				{

					var msg = ReadMessage();
					if (string.IsNullOrEmpty(msg))
					{
						continue;
					}

					lastMessageReceivedTimestamp = DateTime.Now;
					Logger.LogTrace($"> {msg}");

					// Handle the Twitch keep-alive
					if (msg.StartsWith("PING"))
					{
						Logger.LogWarning("Received PING from Twitch... sending PONG");
						SendMessage($"PONG :{msg.Split(':')[1]}");
						continue;
					}

					ProcessMessage(msg);

				} else if (!_TcpClient.Connected)
				{
					// Reconnect
					Logger.LogWarning("Disconnected from Twitch.. Reconnecting in 2 seconds");
					Thread.Sleep(2000);
					this.Init();
					return;
				}

			}

			Logger.LogWarning("Exiting ReceiveMessages Loop");

		}

		private void ProcessMessage(string msg)
		{

			// Logger.LogTrace("Processing message: " + msg);

			var userName = "";
			var message = "";

			userName = ChatClient.reUserName.Match(msg).Groups[1].Value;
			if (userName == Settings.ChatBotName) return; // Exit and do not process if the bot posted this message

			var badges = ChatClient.reBadges.Match(msg).Groups[1].Value.Split(',');

			if (!string.IsNullOrEmpty(userName) && msg.Contains($" JOIN #{ChannelName}"))
			{
				UserJoined?.Invoke(this, new ChatUserJoinedEventArgs { UserName = userName });
			}

			// Review messages sent to the channel
			if (reChatMessage.IsMatch(msg))
			{

				message = ChatClient.reChatMessage.Match(msg).Groups[1].Value;
				Logger.LogTrace($"Message received from '{userName}': {message}");
				NewMessage?.Invoke(this, new NewMessageEventArgs
				{
					UserName = userName,
					Message = message,
					Badges = badges
				});

			} else if (reWhisperMessage.IsMatch(msg))
			{

				message = ChatClient.reWhisperMessage.Match(msg).Groups[1].Value;
				Logger.LogTrace($"Whisper received from '{userName}': {message}");

				NewMessage?.Invoke(this, new NewMessageEventArgs
				{
					UserName = userName,
					Message = message,
					Badges = (badges ?? new string[] { }),
					IsWhisper = true
				});

			}

		}

		private string ReadMessage()
		{

			string message = null;

			try
			{
				message = inputStream.ReadLine();
			} catch (Exception ex)
			{
				Logger.LogError("Error reading messages: " + ex);
			}

			return message ?? "";

		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls
		private Thread _ReceiveMessagesThread;

		protected virtual void Dispose(bool disposing)
		{

			try {
				Logger?.LogWarning("Disposing of ChatClient");
			} catch {}

			if (!disposedValue)
			{
				if (disposing)
				{
					_Shutdown.Cancel();
				}

				_TcpClient?.Dispose();
				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}

	public static class BufferHelpers {

		public static ArraySegment<byte> ToBuffer(this string text)
		{

			return Encoding.UTF8.GetBytes(text);

		}

	}

}
