using System;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace Fritz.Chatbot.Commands
{
  public class ProjectCommand : IBasicCommand2
  {
		private readonly IConfiguration Configuration;
		private readonly IHubContext<ObsHub> _HubContext;

		public ProjectCommand(IConfiguration configuration, IHubContext<ObsHub> hubContext)
		{
			this.Configuration = configuration;
			_HubContext = hubContext;
		}

		public string Trigger => "project";

		public string Description => "Return the name of the project that is currently being worked on on stream";

		public TimeSpan? Cooldown => TimeSpan.FromSeconds(30);

		public static string CurrentProject { get; private set; }

		public async Task Execute(IChatService chatService, string userName, bool isModerator, bool isVip, bool isBroadcaster, ReadOnlyMemory<char> rhs)
		{
			if ((isModerator || isBroadcaster) && !rhs.IsEmpty)
			{
				CurrentProject = rhs.ToString();
				await _HubContext.Clients.All.SendAsync("project_update", CurrentProject);
			}

			var projectText = Configuration["FritzBot:ProjectCommand:TemplateText"];

			var project = CurrentProject;
			if (CurrentProject == null)
				project = Configuration["FritzBot:ProjectCommand:DefaultText"];
			else
				project = CurrentProject;

			await chatService.SendMessageAsync(string.Format(projectText, userName, project));
		}

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			throw new NotImplementedException();
		}
  }
}
