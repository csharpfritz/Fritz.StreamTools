using System;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.SignalR;

namespace Fritz.StreamTools.Hubs
{

	/// So named because @rachelAppel said so..
	public class GithubyMcGithubFace : BaseHub
	{
		public GithubyMcGithubFaceClient GithubyMcGithubFaceClient { get; }

		public GithubyMcGithubFace(
			GithubyMcGithubFaceClient client
			)
		{
			this.GithubyMcGithubFaceClient = client;
		}

		private void Git_Updated(object sender, GitHubNewContributorsEventArgs e)
		{

			this.GithubyMcGithubFaceClient.UpdateGitHub(e.Repository, e.UserName, e.NewCommits);

		}
	}

}
