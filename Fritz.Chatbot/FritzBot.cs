using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fritz.Chatbot.Commands;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fritz.StreamTools.Services
{

	public class FritzBot : IHostedService
	{
		const string QUOTES_FILENAME = "SampleQuotes.txt";
		const char COMMAND_PREFIX = '!';
		readonly IConfiguration _config;
		readonly IServiceProvider _serviceProvider;
		readonly ILogger _logger;
		readonly Random _random = new Random();
		readonly string[] _quotes;
		readonly IChatService[] _chatServices;
		readonly IStreamService[] _streamServices;
		TimeSpan _cooldownTime;
		readonly ConcurrentDictionary<string, ChatUserInfo> _activeUsers = new ConcurrentDictionary<string, ChatUserInfo>();  // Could use IMemoryCache for this ???
		static readonly Dictionary<string, ICommand> _CommandRegistry = new Dictionary<string, ICommand>();

		public FritzBot(IConfiguration config, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
		{
			_config = config;
			_serviceProvider = serviceProvider;
			_logger = loggerFactory.CreateLogger(nameof(FritzBot));
			_chatServices = serviceProvider.GetServices<IChatService>().ToArray();
			_streamServices = serviceProvider.GetServices<IStreamService>().ToArray();

			var cooldownConfig = config["SampleChatBot:CooldownTime"];
			_cooldownTime = !string.IsNullOrEmpty(cooldownConfig) ? TimeSpan.Parse(cooldownConfig) : TimeSpan.Zero;
			_logger.LogInformation("Command cooldown set to {0}", _cooldownTime);


			RegisterCommands();


			if (File.Exists(QUOTES_FILENAME))
			{
				_quotes = File.ReadLines(QUOTES_FILENAME).ToArray();
			}
		}

		private void RegisterCommands()
		{
			
			if (_CommandRegistry.Count > 0)
			{
				return;
			}

			var commandTypes = GetType().Assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(ICommand)));

			foreach (var type in commandTypes)
			{
				if (type.Name == "ICommand") continue;
				var cmd = Activator.CreateInstance(type) as ICommand;
				_CommandRegistry.Add(cmd.Name, cmd);
			}


		}

		#region IHostedService

		public Task StartAsync(CancellationToken cancellationToken)
		{
			foreach (var chat in _chatServices)
			{
				chat.ChatMessage += Chat_ChatMessage;
				chat.UserJoined += Chat_UserJoined;
				chat.UserLeft += Chat_UserLeft;
			}
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			foreach (var chat in _chatServices)
			{
				chat.ChatMessage -= Chat_ChatMessage;
				chat.UserJoined -= Chat_UserJoined;
				chat.UserLeft -= Chat_UserLeft;
			}
			return Task.CompletedTask;
		}

		#endregion

		private async void Chat_ChatMessage(object sender, ChatMessageEventArgs e)
		{
			if (string.IsNullOrEmpty(e.Message) || e.Message[0] != COMMAND_PREFIX)
				return; // e.Message.StartsWith(...) did not work for some reason ?!?
			var segments = e.Message.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (segments.Length == 0)
				return;

			var chatService = sender as IChatService;
			Debug.Assert(chatService != null);
			if (!chatService.IsAuthenticated)
				return;


			var userKey = $"{e.ServiceName}:{e.UserName}";
			if (!_activeUsers.TryGetValue(userKey, out var user))
				user = new ChatUserInfo();

			// Ignore if the normal user is sending commands to fast
			if (CommandsTooFast(e, user, segments[0])) return;

			_logger.LogInformation($"!{segments[0]} from {e.UserName} on {e.ServiceName}");

			// Handle commands
			switch (segments[0].ToLowerInvariant())
			{
				case "ping":
					await chatService.SendWhisperAsync(e.UserName, "pong");
					break;
				case "echo":
					if (segments.Length < 2)
						return;
					await chatService.SendWhisperAsync(e.UserName, "Echo reply: " + string.Join(' ', segments.Skip(1)));
					break;
				case "uptime":
					{
						// Get uptime from the mixer stream service
						var mixer = Array.Find(_streamServices, x => x.Name == "Mixer");
						if (mixer == null)
							break;
						if (mixer.Uptime.HasValue)
							await chatService.SendMessageAsync($"The stream has been up for {mixer.Uptime.Value}");
						else
							await chatService.SendMessageAsync("Stream is offline");
						break;
					}
				case "quote":
					if (_quotes == null)
						break;
					await chatService.SendMessageAsync(_quotes[_random.Next(_quotes.Length)]);
					break;
				default:
					ICommand cmd = null;
					if (_CommandRegistry.TryGetValue(segments[0].ToLowerInvariant(), out cmd)) {
						cmd.ChatService = chatService;
						await cmd.Execute();
					}
						break; // Unknown command
			}

			// Remember last command time
			user.LastCommandTime = DateTime.UtcNow;
			_activeUsers.AddOrUpdate(userKey, user, (k, v) => user);
		}

		private bool CommandsTooFast(ChatMessageEventArgs args, ChatUserInfo user, string namedCommand)
		{

			if (!args.IsModerator && !args.IsOwner)
			{
				if (DateTime.UtcNow - user.LastCommandTime < _cooldownTime)
				{
					_logger.LogWarning($"Ignoring command {namedCommand} from {args.UserName} on {args.ServiceName}. Cooldown active");
					return true;
				}
			}

			return false;
		}

		private void Chat_UserJoined(object sender, ChatUserInfoEventArgs e) => _logger.LogTrace($"{e.UserName} joined {e.ServiceName} chat");
		private void Chat_UserLeft(object sender, ChatUserInfoEventArgs e) => _logger.LogTrace($"{e.UserName} left {e.ServiceName} chat");

	}
}
