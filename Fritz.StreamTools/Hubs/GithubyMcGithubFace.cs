using System;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.SignalR;

namespace Fritz.StreamTools.Hubs
{

	/// So named because @rachelAppel said so..
	public class GithubyMcGithubFace : BaseHub
	{
		public GitHubService GitHubService { get; }
		public GitHubClient GitHubClient { get; }

		public GithubyMcGithubFace(
			GitHubService gitHubService,
			GitHubClient client
			)
		{

			this.GitHubService = gitHubService;
			this.GitHubClient = client;  

			GitHubService.Updated += Git_Updated;

		}

		private void Git_Updated(object sender, GitHubUpdatedEventArgs e)
		{

			this.GitHubClient.UpdateGitHub(e.Contributors);

		}
	}

}
