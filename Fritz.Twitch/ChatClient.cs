using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
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

			Logger.LogError("GC the ChatClient");

			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		public void Init()
		{

			Connect();

			_ReceiveMassagesTask = Task.Factory.StartNew(() => ReceiveMessages(), TaskCreationOptions.LongRunning);

		}

		public ConfigurationSettings Settings { get; }
		public ILogger Logger { get; }

		public string ChannelName => Settings.ChannelName;

		private readonly CancellationTokenSource _Shutdown;

		private void Connect()
		{

			_TcpClient = new TcpClient("irc.chat.twitch.tv", 6667);

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

		private async Task SendMessage(string message, bool flush = true)
		{

			var throttled = CheckThrottleStatus();

			await Task.Delay(throttled.GetValueOrDefault(TimeSpan.FromSeconds(0)));

			await outputStream.WriteLineAsync(message).OrTimeout(2000);
			if (flush)
			{
				await outputStream.FlushAsync();
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

			SendMessage(fullMessage).GetAwaiter().GetResult();

		}

		public void WhisperMessage(string message, string userName)
		{

			var fullMessage = $":{Settings.ChatBotName}!{Settings.ChatBotName}@{Settings.ChatBotName}.tmi.twitch.tv PRIVMSG #jtv :/w {userName} {message}";
			SendMessage(fullMessage).GetAwaiter().GetResult();

		}

		private async Task ReceiveMessages()
		{

			while (true)
			{

				if (_Shutdown.IsCancellationRequested)
				{
					break;
				}

				if (_TcpClient.Connected && _TcpClient.Available > 0)
				{

					var msg = await ReadMessageAsync();
					if (string.IsNullOrEmpty(msg))
					{
						continue;
					}

					Logger.LogTrace($"> {msg}");

					// Handle the Twitch keep-alive
					if (msg.StartsWith("PING"))
					{
						Logger.LogWarning("Received PING from Twitch... sending PONG");
						SendMessage($"PONG :{msg.Split(':')[1]}").GetAwaiter().GetResult();
						continue;
					}

					ProcessMessage(msg);

				} else if (!_TcpClient.Connected)
				{
					// Reconnect
					Logger.LogWarning("Disconnected from Twitch.. Reconnecting in 2 seconds");
					await Task.Delay(2000);
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
					Message = message
				});

			} else if (reWhisperMessage.IsMatch(msg))
			{

				message = ChatClient.reWhisperMessage.Match(msg).Groups[1].Value;
				Logger.LogTrace($"Whisper received from '{userName}': {message}");

				NewMessage?.Invoke(this, new NewMessageEventArgs
				{
					UserName = userName,
					Message = message,
					IsWhisper = true
				});

			}

		}

		private Task<string> ReadMessageAsync()
		{

			Task<string> message = null;

			try
			{
				message = inputStream.ReadLineAsync();
			} catch (Exception ex)
			{
				Logger.LogError("Error reading messages: " + ex);
			}

			return message ?? Task.FromResult("");

		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls


		protected virtual void Dispose(bool disposing)
		{

			Logger.LogWarning("Disposing of ChatClient");

			if (!disposedValue)
			{
				if (disposing)
				{
					_Shutdown.Cancel();
				}

				_TcpClient.Dispose();
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
