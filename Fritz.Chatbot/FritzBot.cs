using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
				const char COMMAND_PREFIX = '!';
				IConfiguration _config;
				ILogger _logger;
				internal IChatService[] _chatServices;
				private AzureQnACommand _qnaCommand;
				readonly ConcurrentDictionary<string, ChatUserInfo> _activeUsers = new ConcurrentDictionary<string, ChatUserInfo>();  // Could use IMemoryCache for this ???
				internal static readonly Dictionary<string, ICommand> _CommandRegistry = new Dictionary<string, ICommand>();

				public TimeSpan CooldownTime { get; private set; }

				public FritzBot(IConfiguration config, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
				{

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

						RegisterCommands();

				}

				private void ConfigureCommandCooldown(IConfiguration config)
				{
						var cooldownConfig = config[$"{CONFIGURATION_ROOT}:CooldownTime"];
						CooldownTime = !string.IsNullOrEmpty(cooldownConfig) ? TimeSpan.Parse(cooldownConfig) : TimeSpan.Zero;
						_logger.LogInformation("Command cooldown set to {0}", CooldownTime);
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

						// Handle Q&A separately
						_CommandRegistry.Remove("qna");
			_CommandRegistry.Remove("ImageDescriptor");

						_qnaCommand = new AzureQnACommand()
						{
								Configuration = _config,
								Logger = _logger
						};

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
						var userKey = $"{e.ServiceName}:{e.UserName}";
						ChatUserInfo user;
						if (!_activeUsers.TryGetValue(userKey, out user))
								user = new ChatUserInfo();

						// message is empty OR message doesn't start with ! AND doesn't end with ?
						if (e.Message.EndsWith("?"))
						{

								_logger.LogInformation($"Handling question: \"{e.Message}\" from {e.UserName} on {e.ServiceName}");


								if (CommandsTooFast("qna")) return;
								await HandleAzureQuestion(e.Message, e.UserName, sender as IChatService);
								return;
						}

						// Check for image processing
						var imageCheckPattern = @"http(s)?:?(\/\/[^""']*\.(?:png|jpg|jpeg|gif))";
						var r = new Regex(imageCheckPattern, RegexOptions.IgnoreCase);

						// Match the regular expression pattern against a text string.
						var imageCheck = r.Match(e.Message);
						if (imageCheck.Captures.Count > 0)
						{
				//cal the new comand
				var imageDescCommand = new ImageDescriptorCommand(_config);
				imageDescCommand.ChatService = sender as IChatService;
				await imageDescCommand.Execute(e.UserName, imageCheck.Captures[0].Value);
				return;

						}

						if (string.IsNullOrEmpty(e.Message) || (e.Message[0] != COMMAND_PREFIX & !e.Message.EndsWith("?")))
								return; // e.Message.StartsWith(...) did not work for some reason ?!?
						var segments = e.Message.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
						if (segments.Length == 0)
								return;

						var chatService = sender as IChatService;
						Debug.Assert(chatService != null);
						if (!chatService.IsAuthenticated)
								return;


						// Ignore if the normal user is sending commands to fast
						if (CommandsTooFast(segments[0])) return;

						_logger.LogInformation($"!{segments[0]} from {e.UserName} on {e.ServiceName}");

						// Handle commands
						ICommand cmd = null;
						if (_CommandRegistry.TryGetValue(segments[0].ToLowerInvariant(), out cmd))
						{
								cmd.ChatService = chatService;
								await cmd.Execute(e.UserName, e.Message);
						}
						else
						{

								await chatService.SendWhisperAsync(e.UserName, "Unknown command.  Try !help for a list of available commands");
								return;
						}

						// Remember last command time
						user.LastCommandTime = DateTime.UtcNow;
						_activeUsers.AddOrUpdate(userKey, user, (k, v) => user);

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



				private async Task HandleAzureQuestion(string message, string userName, IChatService chatService)
				{
						_qnaCommand.ChatService = chatService;
						await _qnaCommand.Execute(userName, message);
						return;
				}


				private void Chat_UserJoined(object sender, ChatUserInfoEventArgs e) => _logger.LogTrace($"{e.UserName} joined {e.ServiceName} chat");

				private void Chat_UserLeft(object sender, ChatUserInfoEventArgs e) => _logger.LogTrace($"{e.UserName} left {e.ServiceName} chat");

		}
}
