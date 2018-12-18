using System;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fritz.Chatbot.Commands
{
	public class AttentionCommand : IBasicCommand, IDisposable
	{
		private readonly IConfiguration Configuration;

		public ILogger Logger { get; }

		public AttentionCommand(IConfiguration configuration, ILoggerFactory loggerFactory)
		{
			this.Configuration = configuration;
			this.Logger = loggerFactory.CreateLogger("AttentionCommand");

			var thisUri = new Uri(configuration["FritzBot:ServerUrl"], UriKind.Absolute);
			var attentionUri = new Uri(thisUri, "attentionhub");

			Logger.LogTrace($"Connecting AttentionCommand to: {attentionUri}");

			this.Client = new HubConnectionBuilder().WithUrl(attentionUri.ToString()).Build();

		}

		protected HubConnection Client { get; }

		public string Trigger => "attention";

		public string Description => "Play audio queue to divert attention to chat";

	public TimeSpan? Cooldown => TimeSpan.Parse(Configuration["FritzBot:AttentionCommand:Cooldown"]);

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			await this.Client.StartAsync();

			await this.Client.InvokeAsync("AlertFritz");

			var attentionText = Configuration["FritzBot:AttentionCommand:TemplateText"];

			await chatService.SendMessageAsync(string.Format(attentionText, userName));
		}

		public void Dispose()
		{
			Client.DisposeAsync();
		}
	}
}
