using System;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Fritz.Chatbot.Commands
{
	public class AttentionCommand : IBasicCommand
	{
		private readonly IConfiguration Configuration;

		public AttentionCommand(IConfiguration configuration)
		{
			this.Configuration = configuration;
			var thisUri = new Uri(configuration["FritzBot:ServerUrl"], UriKind.Absolute);
			this.Client = new HubConnectionBuilder().WithUrl(new Uri(thisUri, "attentionhub").ToString()).Build();
			this.Client.StartAsync();
		}

		protected HubConnection Client { get; }

		public string Trigger => "attention";

		public string Description => "Play audio queue to divert attention to chat";

	public TimeSpan? Cooldown => TimeSpan.Parse(Configuration["FritzBot:AttentionCommand:Cooldown"]);

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			await this.Client.InvokeAsync("AlertFritz");

			var attentionText = Configuration["FritzBot:AttentionCommand:TemplateText"];

			await chatService.SendMessageAsync(string.Format(attentionText, userName));
		}
	}
}
