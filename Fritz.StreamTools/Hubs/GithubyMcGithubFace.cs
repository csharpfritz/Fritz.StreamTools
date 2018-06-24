using System;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.SignalR;

namespace Fritz.StreamTools.Hubs
{

	/// So named because @rachelAppel said so..
	public class GithubyMcGithubFace : BaseHub
	{
		public GitHubClient GitHubClient { get; }

		public GithubyMcGithubFace(
			GitHubClient client
			)
		{
			this.GitHubClient = client;
		}

		private void Git_Updated(object sender, GitHubUpdatedEventArgs e)
		{

			this.GitHubClient.UpdateGitHub(e.Contributors);

		}
	}

}
