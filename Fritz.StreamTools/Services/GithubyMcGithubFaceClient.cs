using System.Collections.Generic;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Fritz.StreamTools.Services
{
<<<<<<< HEAD:Fritz.StreamTools/Services/GitHubClient.cs
  public class GitHubClient
=======
	public class GithubyMcGithubFaceClient
>>>>>>> 51f144d29a8b42000ebd8d2a641f0517a7aaa6f1:Fritz.StreamTools/Services/GithubyMcGithubFaceClient.cs
	{

		public GithubyMcGithubFaceClient(IHubContext<GithubyMcGithubFace> mcGitHubContext)
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
