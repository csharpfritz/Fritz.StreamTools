using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System;

namespace Fritz.Chatbot.Commands
{
  public class TopCommand : IBasicCommand
  {
		private readonly HttpClient _Client;

		public TopCommand() { }

		public TopCommand(IConfiguration config, HttpClient client)
		{
			client.BaseAddress = new Uri( config["StreamUrl"]);
			_Client = client;
		}

		public string Trigger => "Top";
		public string Description => "Get top contributors from github";

		public TimeSpan? Cooldown => null;

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			var response = await _Client.GetAsync("/Github/ContributorsInformationApi");
			var result = await response.Content.ReadAsStringAsync();
			response.EnsureSuccessStatusCode();

			var model = JsonConvert.DeserializeObject<IEnumerable<GitHubInformation>>(result);

			await chatService.SendMessageAsync(GetMsg(model));
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
			message = builder.ToString();
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
