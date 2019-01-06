using System;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;

namespace Fritz.Chatbot.Commands
{
  public class ProjectCommand : IBasicCommand2
  {
		private readonly IConfiguration Configuration;

		public ProjectCommand(IConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		public string Trigger => "project";

		public string Description => "Return the name of the project that is currently being worked on on stream";

		public TimeSpan? Cooldown => TimeSpan.FromSeconds(30);

		public async Task Execute(IChatService chatService, string userName, bool isModerator, bool isBroadcaster, ReadOnlyMemory<char> rhs)
		{
			if ((isModerator || isBroadcaster) && !rhs.IsEmpty)
			{
				chatService.CurrentProject = rhs.ToString();
			}

			var projectText = Configuration["FritzBot:ProjectCommand:TemplateText"];

			var project = chatService.CurrentProject;
			if (chatService.CurrentProject == null)
				project = Configuration["FritzBot:ProjectCommand:DefaultText"];
			else
				project = chatService.CurrentProject;

			await chatService.SendMessageAsync(string.Format(projectText, userName, project));
		}

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			throw new NotImplementedException();
		}
  }
}
