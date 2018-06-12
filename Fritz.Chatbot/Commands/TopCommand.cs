using System.Net.Http;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Fritz.Chatbot.Commands
{
  public class TopCommand : ICommand
  {
		private readonly string _StreamUrl;
		public IChatService ChatService { get; set; }

		public TopCommand() { }

		public TopCommand(IConfiguration config)
		{
			_StreamUrl = config["StreamUrl"];
		}

		public string Name => "Top";
		public string Description => "Get top contributors from github";

		public async Task Execute(string userName, string fullCommandText)
		{
			var client = new HttpClient();
			var response = await client.GetAsync(_StreamUrl);
			var result = await response.Content.ReadAsStringAsync();
			response.EnsureSuccessStatusCode();

			var model = JsonConvert.DeserializeObject<IEnumerable<GitHubInformation>>(result);

			await ChatService.SendMessageAsync(GetMsg(model));
		}

		private string GetMsg(IEnumerable<GitHubInformation> model)
		{
			var message = string.Empty;
			var builder = new StringBuilder();
			builder.AppendLine("Top contributors: ");
			foreach(var repoInfo in model)
			{
				builder.Append("Repository: ");
				builder.AppendLine(repoInfo.Repository);
				builder.AppendLine("Top week contributors:");
				AppendContributors(builder, repoInfo.TopWeekContributors);
				builder.AppendLine("Top month contributors:");
				AppendContributors(builder, repoInfo.TopMonthContributors);
				builder.AppendLine("Top ever contributors:");
				AppendContributors(builder, repoInfo.TopEverContributors);
			}

			return message;
		}

		private void AppendContributors(StringBuilder builder, IEnumerable<GitHubContributor> contributors)
		{
			foreach(var item in contributors)
			{
				builder.AppendLine($"{item.Author}, with {item.Commits} commits!");
			}
		}
  }
}
