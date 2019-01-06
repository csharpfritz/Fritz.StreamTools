using System;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fritz.Chatbot.Commands
{
	public class AttentionCommand : IBasicCommand
	{
		private readonly IConfiguration Configuration;

		public ILogger Logger { get; }
		public IHubContext<AttentionHub, IAttentionHubClient> HubContext { get; }

		public AttentionCommand(IConfiguration configuration, IHubContext<AttentionHub, IAttentionHubClient> hubContext, ILoggerFactory loggerFactory)
		{
			this.Configuration = configuration;
			this.Logger = loggerFactory.CreateLogger("AttentionCommand");

			this.HubContext = hubContext;

			//var thisUri = new Uri(configuration["FritzBot:ServerUrl"], UriKind.Absolute);
			//var attentionUri = new Uri(thisUri, "attentionhub");

			//Logger.LogTrace($"Connecting AttentionCommand to: {attentionUri}");

			//this.Client = new HubConnectionBuilder().WithUrl(attentionUri.ToString()).Build();

		}

		//protected HubConnection Client { get; }

		public string Trigger => "attention";

		public string Description => "Play audio queue to divert attention to chat";

#if DEBUG
		public TimeSpan? Cooldown => TimeSpan.FromSeconds(10);
#else
	public TimeSpan? Cooldown => TimeSpan.Parse(Configuration["FritzBot:AttentionCommand:Cooldown"]);
#endif

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			await this.HubContext.Clients.All.AlertFritz();

			var attentionText = Configuration["FritzBot:AttentionCommand:TemplateText"];

			await chatService.SendMessageAsync(string.Format(attentionText, userName));
		}

	}
}
