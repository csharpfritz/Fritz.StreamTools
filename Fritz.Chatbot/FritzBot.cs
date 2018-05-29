using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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

		public const string CONFIGURATION_ROOT = "FritzBot";
		IConfiguration _config;
		ILogger _logger;
		internal IChatService[] _chatServices;
		private IBasicCommand[] _basicCommands;
		private IExtendedCommand[] _extendedCommands;
		readonly ConcurrentDictionary<string, ChatUserInfo> _activeUsers = new ConcurrentDictionary<string, ChatUserInfo>(); // Could use IMemoryCache for this ???
		private readonly IServiceProvider _serviceProvider;
		readonly ConcurrentDictionary<string, DateTime> _commandExecutedTimeMap = new ConcurrentDictionary<string, DateTime>();

		public TimeSpan CooldownTime { get; private set; }

		public FritzBot(IConfiguration config, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
		{

			_serviceProvider = serviceProvider;
			var chatServices = serviceProvider.GetServices<IChatService>().ToArray();
			Initialize(config, chatServices, loggerFactory);
		}

		internal FritzBot() { }

		internal void Initialize(IConfiguration config, IChatService[] chatServices, ILoggerFactory loggerFactory)
		{

			_config = config;
			_logger = loggerFactory.CreateLogger(nameof(FritzBot));
			_chatServices = chatServices;

			ConfigureCommandCooldown(config);

			_basicCommands = _serviceProvider.GetServices<IBasicCommand>().ToArray();
			_extendedCommands = _serviceProvider.GetServices<IExtendedCommand>().OrderBy(k => k.Order).ToArray();
		}

		private void ConfigureCommandCooldown(IConfiguration config)
		{
			var cooldownConfig = config[$"{CONFIGURATION_ROOT}:CooldownTime"];
			CooldownTime = !string.IsNullOrEmpty(cooldownConfig) ? TimeSpan.Parse(cooldownConfig) : TimeSpan.Zero;
			_logger.LogInformation("Command cooldown set to {0}", CooldownTime);
		}

		#region Static stuff

		/// <summary>
		/// Register all classes derived from IBasicCommand & IExtendedCommand as singletons in DI
		/// </summary>
		public static void RegisterCommands(IServiceCollection services)
		{
			// Register basic commands
			foreach (var type in typeof(FritzBot).Assembly.GetTypes().Where(t => typeof(IBasicCommand).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass))
				services.AddSingleton(typeof(IBasicCommand), type);

			// Register extended commands
			foreach (var type in typeof(FritzBot).Assembly.GetTypes().Where(t => typeof(IExtendedCommand).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass))
				services.AddSingleton(typeof(IExtendedCommand), type);
		}

		#endregion

		#region IHostedService

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

		#endregion

		private async void OnChat_ChatMessage(object sender, ChatMessageEventArgs e)
		{
			// async void as Event callback
			try
			{
				await ProcessChatMessage(sender, e);
			}
			catch (Exception ex)
			{
				// Don't let exception escape from async void
				_logger.LogError($"{DateTime.UtcNow}: Chat_ChatMessage - Error {Environment.NewLine}{ex}");
			}
		}

		private async Task ProcessChatMessage(object sender, ChatMessageEventArgs e)
		{

			// TODO: Add queue processing to ensure only one instance of a command is executing at a time

			var userKey = $"{e.ServiceName}:{e.UserName}";
			var user = _activeUsers.AddOrUpdate(userKey, new ChatUserInfo(), (_, u) => u);

			var chatService = sender as IChatService;

			var final = await HandleExtendedCommands();
			if (final)
				return;

			if (e.Message.FirstOrDefault() == '!')
			{
				if (!await HandleBasicCommands())
				{
					await chatService.SendWhisperAsync(e.UserName, "Unknown command.  Try !help for a list of available commands");
				}
			}

			return; // Only local functions below

			async ValueTask<bool> HandleBasicCommands()
			{
				// NOTE: Returns true if the command was found

				Debug.Assert(_basicCommands != null);
				Debug.Assert(!string.IsNullOrEmpty(e.Message) && e.Message[0] == '!');

				var trigger = e.Message.AsMemory(1);
				var rhs = ReadOnlyMemory<char>.Empty;
				int n = trigger.Span.IndexOf(' ');
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
						if (CommandsTooFast(cmd.Trigger, cmd.Cooldown))
							return true;

						await cmd.Execute(chatService, e.UserName, rhs);

						AfterExecute(cmd.Trigger);
						return true;
					}
				}

				return false;
			}

			async ValueTask<bool> HandleExtendedCommands()
			{
				// NOTE: Returns true if no other commands should be run

				Debug.Assert(_extendedCommands != null);

				foreach (var cmd in _extendedCommands)
				{
					Debug.Assert(!string.IsNullOrEmpty(cmd.Name));

					if (cmd.CanExecute(e.UserName, e.Message))
					{
						// Ignore if the normal user is sending commands to fast, or command is in cooldown
						if (CommandsTooFast(cmd.Name, cmd.Cooldown))
							return false;

						await cmd.Execute(chatService, e.UserName, e.Message);

						AfterExecute(cmd.Name);
						return cmd.Final;
					}
				}

				return false;
			}

			bool CommandsTooFast(string namedCommand, TimeSpan? cooldown = null)
			{
				Debug.Assert(user != null);

#if !DEBUG
				if (e.IsModerator || e.IsOwner)
					return false;
#endif

				// Check per user cooldown
				if (DateTime.UtcNow - user.LastCommandTime < CooldownTime)
				{
					_logger.LogWarning("Ignoring command {0} from {1} on {2}. Cooldown active", namedCommand, e.UserName, e.ServiceName);
					return true;
				}

				// Check per command cooldown
				if (_commandExecutedTimeMap.TryGetValue(namedCommand, out var dt))
				{
					var now = DateTime.UtcNow;
					if (now - dt < cooldown.GetValueOrDefault())
					{
						var remain = cooldown.GetValueOrDefault() - (now - dt);
						_logger.LogWarning("Ignoring command {0} from {1} on {2}. In cooldown for {3} more secs", namedCommand, e.UserName, e.ServiceName,
							(int) remain.TotalSeconds);
						return true;
					}
				}

				return false;
			}

			void AfterExecute(string command)
			{
				Debug.Assert(user != null);

				// Remember last command time
				user.LastCommandTime = DateTime.UtcNow;
				_commandExecutedTimeMap[command] = DateTime.UtcNow;
			}
		}

		// private async Task HandleAzureQuestion(string message, string userName, IChatService chatService)
		// {
		// 		_qnaCommand.ChatService = chatService;
		// 		await _qnaCommand.Execute(userName, message);
		// 		return;
		// }

		private void Chat_UserJoined(object sender, ChatUserInfoEventArgs e) => _logger.LogTrace($"{e.UserName} joined {e.ServiceName} chat");

		private void Chat_UserLeft(object sender, ChatUserInfoEventArgs e) => _logger.LogTrace($"{e.UserName} left {e.ServiceName} chat");

	}
}
