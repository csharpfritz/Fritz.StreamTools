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
	public class ScottCommand : IBasicCommand
	{
		private readonly IConfiguration Configuration;

		public ILogger Logger { get; }

		public IHubContext<AttentionHub, IAttentionHubClient> HubContext { get; }

		public ScottCommand(IConfiguration configuration, IHubContext<AttentionHub, IAttentionHubClient> hubContext, ILoggerFactory loggerFactory)
		{
			this.Configuration = configuration;
			this.Logger = loggerFactory.CreateLogger("ScottCommand");

			this.HubContext = hubContext;

		}

		public string Trigger => "scott";

		public string Description => "Request Frau Farbissina to summon Scott";

		public TimeSpan? Cooldown => TimeSpan.FromSeconds(60);

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			await this.HubContext.Clients.All.SummonScott();

			await chatService.SendMessageAsync(string.Format("SCOTTTTTT!!!", userName));

		}

	}
}
