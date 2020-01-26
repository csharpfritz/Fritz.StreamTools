using Fritz.Chatbot.Commands;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.Chatbot
{
	public class FritzBot : IHostedService
	{
		public const string ConfigurationRoot = "FritzBot";
		public const string UnknownCommandMessage = "Unknown command. Try !help for a list of available commands.";

		private readonly ILogger _logger;
		private readonly IChatService[] _chatServices;
		private readonly IBasicCommand[] _basicCommands;
		private readonly IExtendedCommand[] _extendedCommands;
		internal static string[] _OtherBots;

		private readonly ConcurrentDictionary<string, ChatUserInfo> _activeUsers = new ConcurrentDictionary<string, ChatUserInfo>();
		private readonly ConcurrentDictionary<string, DateTime> _commandExecutedTimeMap = new ConcurrentDictionary<string, DateTime>();

		public TimeSpan CooldownTime { get; }

		public FritzBot(IConfiguration configuration, IServiceProvider serviceProvider, ILoggerFactory loggerFactory = null)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}

			if (serviceProvider == null)
			{
				throw new ArgumentNullException(nameof(serviceProvider));
			}

			_chatServices = serviceProvider.GetServices<IChatService>().ToArray();

			_basicCommands = serviceProvider.GetServices<IBasicCommand>().ToArray();
			_extendedCommands = serviceProvider.GetServices<IExtendedCommand>().OrderBy(command => command.Order).ToArray();

			_logger = loggerFactory?.CreateLogger(nameof(FritzBot));

			var cooldownConfig = configuration[$"{ConfigurationRoot}:CooldownTime"];
			CooldownTime = !string.IsNullOrEmpty(cooldownConfig) ? TimeSpan.Parse(cooldownConfig) : TimeSpan.Zero;

			_OtherBots = String.IsNullOrEmpty(configuration[$"{ConfigurationRoot}:Otherbots"]) ? new[] { "nightbot","fritzbot","streamelements","pretzelrocks" } : configuration[$"{ConfigurationRoot}:Otherbots"].Split(',');

			_logger?.LogInformation("Command cooldown set to {0}", CooldownTime);
		}

		/// <summary>
		/// Register all classes derived from IBasicCommand & IExtendedCommand as singletons in DI
		/// </summary>
		public static void RegisterCommands(IServiceCollection services)
		{
			// Register basic commands
			foreach (var type in typeof(FritzBot).Assembly.GetTypes()
																						.Where(t => typeof(IBasicCommand)
																						.IsAssignableFrom(t) && !t.IsAbstract && t.IsClass))
			{
				services.AddSingleton(typeof(IBasicCommand), type);
			}

			// Register extended commands
			foreach (var type in typeof(FritzBot).Assembly.GetTypes()
																						.Where(t => typeof(IExtendedCommand)
																						.IsAssignableFrom(t) && !t.IsAbstract && t.IsClass))
			{
				services.AddSingleton(typeof(IExtendedCommand), type);
			}
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			foreach (var chat in _chatServices)
			{
				chat.ChatMessage += OnChat_ChatMessage;
				chat.UserJoined += Chat_UserJoined;
				chat.UserLeft += Chat_UserLeft;
			}

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			foreach (var chat in _chatServices)
			{
				chat.ChatMessage -= OnChat_ChatMessage;
				chat.UserJoined -= Chat_UserJoined;
				chat.UserLeft -= Chat_UserLeft;
			}

			return Task.CompletedTask;
		}

		private async void OnChat_ChatMessage(object sender, ChatMessageEventArgs chatMessageArgs)
		{
			try
			{
				await ProcessChatMessage(sender, chatMessageArgs);
			}
			catch (Exception ex)
			{
				_logger?.LogError($"{DateTime.UtcNow}: Chat_ChatMessage - Error {Environment.NewLine}{ex}");
			}
		}

		private async Task ProcessChatMessage(object sender, ChatMessageEventArgs chatMessageArgs)
		{
			if (!(sender is IChatService))
			{
				return;
			}

			// TODO: Add queue processing to ensure only one instance of a command is executing at a time

			var userKey = $"{chatMessageArgs.ServiceName}:{chatMessageArgs.UserName}";
			var user = _activeUsers.AddOrUpdate(userKey, new ChatUserInfo(), (_, u) => u);
			var chatService = sender as IChatService;
			if (chatService.BotUserName.Equals(chatMessageArgs.UserName, StringComparison.InvariantCultureIgnoreCase)) return; // Don't process my own messages

			if (await HandleExtendedCommands(chatService, chatMessageArgs, user))
			{
				_logger.LogDebug($"Handled with extended command: {chatMessageArgs.Message}");
				return;
			}

			if (chatMessageArgs.Message.FirstOrDefault() == '!' && !TextCommand.IsCommand(chatMessageArgs.Message.Substring(1).Split(' ')[0]))
			{
				if (!await HandleBasicCommands(chatService, chatMessageArgs, user))
				{
					await chatService.SendWhisperAsync(chatMessageArgs.UserName, UnknownCommandMessage);
				}
			}
			else
			{

				// TODO: Capture and transmit the user who sent the message
				//var otherBots = new[] { "nightbot", "fritzbot", "streamelements", "pretzelrocks" };
				if (!_OtherBots.Contains(chatMessageArgs.UserName.ToLowerInvariant()))
				{
					SentimentSink.RecentChatMessages.Enqueue(chatMessageArgs.Message);
				}
			}
		}

		private async ValueTask<bool> HandleBasicCommands(IChatService chatService, ChatMessageEventArgs chatMessageArgs, ChatUserInfo user)
		{
			Debug.Assert(_basicCommands != null);
			Debug.Assert(!string.IsNullOrEmpty(chatMessageArgs.Message) && chatMessageArgs.Message[0] == '!');

			var trigger = chatMessageArgs.Message.AsMemory(1);
			var rhs = ReadOnlyMemory<char>.Empty;
			var n = trigger.Span.IndexOf(' ');
			if (n != -1)
			{
				rhs = trigger.Slice(n + 1);
				trigger = trigger.Slice(0, n);
			}

			foreach (var cmd in _basicCommands)
			{
				Debug.Assert(!string.IsNullOrEmpty(cmd.Trigger));

				if (trigger.Span.Equals(cmd.Trigger.AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					// Ignore if the normal user is sending commands to fast, or command is in cooldown
					if (CommandsTooFast(chatMessageArgs, user, cmd.Trigger, cmd.Cooldown))
					{
						_logger.LogDebug($"CommandTooFast: {cmd.Trigger}");
						return true;
					}

					if (cmd is IBasicCommand2)
					{
						await (cmd as IBasicCommand2).Execute(chatService, chatMessageArgs.UserName, chatMessageArgs.IsModerator, chatMessageArgs.IsVip, chatMessageArgs.IsOwner, rhs);
					}
					else
					{
						await cmd.Execute(chatService, chatMessageArgs.UserName, rhs);
					}

					AfterExecute(user, cmd.Trigger);

					return true;
				}
			}

			return false;
		}

		private async ValueTask<bool> HandleExtendedCommands(IChatService chatService, ChatMessageEventArgs chatMessageArgs, ChatUserInfo user)
		{
			Debug.Assert(_extendedCommands != null);

			foreach (var cmd in _extendedCommands)
			{
				Debug.Assert(!string.IsNullOrEmpty(cmd.Name));

				if (cmd.CanExecute(chatMessageArgs.UserName, chatMessageArgs.Message))
				{
					// Ignore if the normal user is sending commands to fast, or command is in cooldown
					if (CommandsTooFast(chatMessageArgs, user, cmd.Name, cmd.Cooldown))
					{
						return false;
					}

					await cmd.Execute(chatService, chatMessageArgs.UserName, chatMessageArgs.Message);

					AfterExecute(user, cmd.Name);

					if (cmd.Final) return true;
				}
			}

			return false;
		}

		private bool CommandsTooFast(ChatMessageEventArgs chatMessageArgs, ChatUserInfo user, string namedCommand, TimeSpan? cooldown = null)
		{
			Debug.Assert(user != null);

			if (chatMessageArgs.IsModerator || chatMessageArgs.IsOwner)
			{
				return false;
			}

			var now = DateTime.UtcNow;
			if (now - user.LastCommandTime < CooldownTime)
			{
				_logger?.LogWarning($"Ignoring command {namedCommand} from {chatMessageArgs.UserName} on {chatMessageArgs.ServiceName}. Cooldown active");

				return true;
			}

			if (_commandExecutedTimeMap.TryGetValue(namedCommand, out var dt)
						&&
					now - dt < cooldown.GetValueOrDefault())
			{
				var remain = cooldown.GetValueOrDefault() - (now - dt);
				_logger?.LogWarning($"Ignoring command {namedCommand} from {chatMessageArgs.UserName} on {chatMessageArgs.ServiceName}. In cooldown for {(int)remain.TotalSeconds} more secs");

				return true;
			}

			return false;
		}

		private void AfterExecute(ChatUserInfo user, string command)
		{
			Debug.Assert(user != null);

			// Remember last command time
			user.LastCommandTime = DateTime.UtcNow;
			_commandExecutedTimeMap[command] = DateTime.UtcNow;
		}

		private void Chat_UserJoined(object sender, ChatUserInfoEventArgs e) => _logger?.LogTrace($"{e.UserName} joined {e.ServiceName} chat");

		private void Chat_UserLeft(object sender, ChatUserInfoEventArgs e) => _logger?.LogTrace($"{e.UserName} left {e.ServiceName} chat");
	}
}
