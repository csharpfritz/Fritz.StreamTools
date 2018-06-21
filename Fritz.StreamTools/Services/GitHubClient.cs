using System.Collections.Generic;
using Fritz.StreamTools.Hubs;
using Fritz.StreamTools.Models;
using Microsoft.AspNetCore.SignalR;

namespace Fritz.StreamTools.Services
{
	public class GitHubClient
	{

		public GitHubClient(IHubContext<GithubyMcGithubFace> mcGitHubContext)
		{

			this.McGitHubContext = mcGitHubContext;

		}

		private IHubContext<GithubyMcGithubFace> McGitHubContext { get; }

		internal void UpdateGitHub(string repository, string userName, int commits)
		{
			McGitHubContext.Clients.Group("github").SendAsync("OnGitHubUpdated", repository, userName, commits);
		}

	}

}
