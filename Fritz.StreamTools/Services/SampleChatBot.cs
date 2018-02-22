using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fritz.StreamTools.Services
{
	public class SampleChatBot : IHostedService
	{
		readonly IServiceProvider _serviceProvider;
		readonly ILogger _logger;
		private IChatService[] _chatServices;

		public SampleChatBot(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
		{
			_serviceProvider = serviceProvider;
			_logger = loggerFactory.CreateLogger<SampleChatBot>();
			_chatServices = serviceProvider.GetServices<IChatService>().ToArray();
		}

		#region IHostedService

		public Task StartAsync(CancellationToken cancellationToken)
		{
			foreach(var chat in _chatServices)
			{
				chat.OnChatMessage += Chat_OnChatMessage;
			}
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			foreach (var chat in _chatServices)
			{
				chat.OnChatMessage -= Chat_OnChatMessage;
			}
			return Task.CompletedTask;
		}

		#endregion

		private async void Chat_OnChatMessage(object sender, ChatMessageEventArgs e)
		{
			if (!e.Message.StartsWith('!')) return;
			var segments = e.Message.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (!segments.Any()) return;

			var chatService = sender as IChatService;

			_logger.LogInformation($"!{segments[0]} from {e.UserName} on {e.ServiceName}");

			switch (segments[0].ToLowerInvariant())
			{
				case "ping":
					await chatService.SendWhisperAsync(e.UserName, "BOT: pong");
					break;
				case "echo":
					if (segments.Length < 2) return;
					await chatService.SendMessageAsync("BOT: " + string.Join(' ', segments.Skip(1)));
					break;
				case "uptime":
					await chatService.SendMessageAsync("BOT: The stream has be up some time :)");
					break;
			}
		}
	}
}
