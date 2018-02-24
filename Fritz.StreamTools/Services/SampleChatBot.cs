using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fritz.StreamTools.Services
{
	public class ChatUserInfo
	{
		public DateTime LastCommandTime { get; set; }
	}

	public class SampleChatBot : IHostedService
	{
		const string QUOTES_FILENAME = "SampleQuotes.txt";
		readonly IConfiguration _config;
		readonly IServiceProvider _serviceProvider;
		readonly ILogger _logger;
		readonly Random _random = new Random();
		readonly string[] _quotes;
		IChatService[] _chatServices;
		readonly IStreamService[] _streamServices;
		TimeSpan _cooldownTime;
		ConcurrentDictionary<string, ChatUserInfo> _activeUsers = new ConcurrentDictionary<string, ChatUserInfo>();	// Could use IMemoryCache for this ???

		public SampleChatBot(IConfiguration config, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
		{
			_config = config;
			_serviceProvider = serviceProvider;
			_logger = loggerFactory.CreateLogger<SampleChatBot>();
			_chatServices = serviceProvider.GetServices<IChatService>().ToArray();
			_streamServices = serviceProvider.GetServices<IStreamService>().ToArray();

			var cooldownConfig = config["SampleChatBot:CooldownTime"];
			_cooldownTime = !string.IsNullOrEmpty(cooldownConfig) ? TimeSpan.Parse(cooldownConfig) : TimeSpan.Zero;
			_logger.LogInformation("Command cooldown set to {0}", _cooldownTime);

			if (File.Exists(QUOTES_FILENAME))
			{
				_quotes = File.ReadLines(QUOTES_FILENAME).ToArray();
			}
		}

		#region IHostedService

		public Task StartAsync(CancellationToken cancellationToken)
		{
			foreach(var chat in _chatServices)
			{
				chat.ChatMessage += Chat_ChatMessage;
			}
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			foreach (var chat in _chatServices)
			{
				chat.ChatMessage -= Chat_ChatMessage;
			}
			return Task.CompletedTask;
		}

		#endregion

		private async void Chat_ChatMessage(object sender, ChatMessageEventArgs e)
		{
			if (!e.Message.StartsWith('!')) return;
			var segments = e.Message.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (!segments.Any()) return;

			var chatService = sender as IChatService;
			Debug.Assert(chatService != null);

			// Ignore if the normal user is sending commands to fast
			var userKey = $"{e.ServiceName}:{e.UserName}";
			if (!_activeUsers.TryGetValue(userKey, out var user))
				user = new ChatUserInfo();
			if (!e.IsModerator && !e.IsOwner)
			{
				if (DateTime.UtcNow - user.LastCommandTime < _cooldownTime)
				{
					_logger.LogWarning($"Ignoring command {segments[0]} from {e.UserName} on {e.ServiceName}. Cooldown active");
					return;
				}
			}

			_logger.LogInformation($"!{segments[0]} from {e.UserName} on {e.ServiceName}");

			switch (segments[0].ToLowerInvariant())
			{
				case "help":
					await chatService.SendMessageAsync("Supported commands: !ping !echo !uptime !quote");
					break;
				case "ping":
					await chatService.SendWhisperAsync(e.UserName, "pong");
					break;
				case "echo":
					if (segments.Length < 2) return;
					await chatService.SendMessageAsync("Echo reply: " + string.Join(' ', segments.Skip(1)));
					break;
				case "uptime":
					{
						// Get uptime from the mixer stream service
						var mixer = _streamServices.Where(x => x.Name == "Mixer").FirstOrDefault();
						if (mixer == null) break;
						if (mixer.Uptime.HasValue)
							await chatService.SendMessageAsync($"The stream has been up for {mixer.Uptime.Value}");
						else
							await chatService.SendMessageAsync("Stream is offline");
						break;
					}
				case "quote":
					if (_quotes == null) break;
					await chatService.SendMessageAsync(_quotes[_random.Next(_quotes.Length)]);
					break;
				default:
					return;	// Unknown command
			}

			// Remember last command time
			user.LastCommandTime = DateTime.UtcNow;
			_activeUsers.AddOrUpdate(userKey, user, (k, v) => user);
		}
	}
}
