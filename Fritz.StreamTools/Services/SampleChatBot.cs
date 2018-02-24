using System;
using System.IO;
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
		const string QUOTES_FILENAME = "SampleQuotes.txt";
		readonly IServiceProvider _serviceProvider;
		readonly ILogger _logger;
		readonly Random _random = new Random();
		readonly string[] _quotes;
		IChatService[] _chatServices;
		readonly IStreamService[] _streamServices;

		public SampleChatBot(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
		{
			_serviceProvider = serviceProvider;
			_logger = loggerFactory.CreateLogger<SampleChatBot>();
			_chatServices = serviceProvider.GetServices<IChatService>().ToArray();
			_streamServices = serviceProvider.GetServices<IStreamService>().ToArray();

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

			_logger.LogInformation($"!{segments[0]} from {e.UserName} on {e.ServiceName}");

			switch (segments[0].ToLowerInvariant())
			{
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
						if(mixer.Uptime.HasValue)
							await chatService.SendMessageAsync($"The stream has been up for {mixer.Uptime.Value}");
						break;
					}
				case "quote":
					if (_quotes == null) break;
					await chatService.SendMessageAsync(_quotes[_random.Next(_quotes.Length)]);
					break;
			}
		}
	}
}
