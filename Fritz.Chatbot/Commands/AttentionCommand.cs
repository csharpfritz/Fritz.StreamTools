using System;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;


namespace Fritz.Chatbot.Commands
{
	public class AttentionCommand : IBasicCommand
	{
		private readonly IConfiguration Configuration;

		public AttentionCommand(IAttentionClient client, IConfiguration configuration)
		{
			this.Configuration = configuration;
			this.Client = client;
		}

		protected IAttentionClient Client { get; }

		public string Trigger => "attention";

		public string Description => "Play audio queue to divert attention to chat";

	public TimeSpan? Cooldown => TimeSpan.Parse(Configuration["FritzBot:AttentionCommand:Cooldown"]);

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			await this.Client.AlertFritz();

			var attentionText = Configuration["FritzBot:AttentionCommand:TemplateText"];

			var sb = new StringBuilder();
			sb.AppendFormat(attentionText, userName);

			await chatService.SendMessageAsync(attentionText);
		}
	}
}
