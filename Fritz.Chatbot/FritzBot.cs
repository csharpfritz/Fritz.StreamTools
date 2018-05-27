using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
		readonly ConcurrentDictionary<string, ChatUserInfo> _activeUsers = new ConcurrentDictionary<string, ChatUserInfo>(); // Could use IMemoryCache for this ???
		private readonly IServiceProvider _serviceProvider;

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

		}

		private void ConfigureCommandCooldown(IConfiguration config)
		{
			var cooldownConfig = config[$"{CONFIGURATION_ROOT}:CooldownTime"];
			CooldownTime = !string.IsNullOrEmpty(cooldownConfig) ? TimeSpan.Parse(cooldownConfig) : TimeSpan.Zero;
			_logger.LogInformation("Command cooldown set to {0}", CooldownTime);
		}

		/// <summary>
		/// Register all classes derived from ICommand singletons in DI
		/// NOTE: ICommand derived classes should never store any states!
		/// </summary>
		public static void RegisterCommands(IServiceCollection services)
		{
			var commandTypes = typeof(FritzBot).Assembly.GetTypes().Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);
			foreach (var type in commandTypes)
				services.AddSingleton(typeof(ICommand), type);
		}

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
				await Chat_ChatMessage(sender, e);
			}
			catch (Exception ex)
			{
				// Don't let exception escape from async void
				_logger.LogError($"{DateTime.UtcNow}: Chat_ChatMessage - Error {Environment.NewLine}{ex}");
			}
		}

		private async Task Chat_ChatMessage(object sender, ChatMessageEventArgs e)
		{

			// TODO: Add queue processing to ensure only one instance of a command is executing at a time

			var userKey = $"{e.ServiceName}:{e.UserName}";
			ChatUserInfo user;
			if (!_activeUsers.TryGetValue(userKey, out user))
				user = new ChatUserInfo();

			var chatService = sender as IChatService;

			foreach (var cmd in _serviceProvider.GetServices<ICommand>().OrderBy(k => k.Order))
			{

				if (cmd.CanExecute(e.UserName, e.Message))
				{

					// Ignore if the normal user is sending commands to fast
					if (CommandsTooFast(cmd.Name)) return;

					await cmd.Execute(chatService, e.UserName, e.Message);

					// Remember last command time
					user.LastCommandTime = DateTime.UtcNow;
					_activeUsers.AddOrUpdate(userKey, user, (k, v) => user);

					return;
				}

			}

			await chatService.SendWhisperAsync(e.UserName, "Unknown command.  Try !help for a list of available commands");

			return;

			// if (string.IsNullOrEmpty(e.Message) || (e.Message[0] != COMMAND_PREFIX & !e.Message.EndsWith("?")))
			// 		return; // e.Message.StartsWith(...) did not work for some reason ?!?
			// var segments = e.Message.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
			// if (segments.Length == 0)
			// 		return;

			// var chatService = sender as IChatService;
			// Debug.Assert(chatService != null);
			// if (!chatService.IsAuthenticated)
			// 		return;

			// 					_logger.LogInformation($"!{segments[0]} from {e.UserName} on {e.ServiceName}");

			// // Handle commands
			// ICommand cmd = null;
			// if (_CommandRegistry.TryGetValue("!" + segments[0].ToLowerInvariant(), out cmd))
			// {
			// 		cmd.ChatService = chatService;
			// 		await cmd.Execute(e.UserName, e.Message);
			// }
			// else
			// {

			bool CommandsTooFast(string namedCommand)
			{

				if (!e.IsModerator && !e.IsOwner)
				{
					if (DateTime.UtcNow - user.LastCommandTime < CooldownTime)
					{
						_logger.LogWarning($"Ignoring command {namedCommand} from {e.UserName} on {e.ServiceName}. Cooldown active");
						return true;
					}
				}

				return false;
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
