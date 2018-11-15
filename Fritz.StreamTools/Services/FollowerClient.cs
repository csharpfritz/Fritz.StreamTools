// REF: https://dev.mixer.com/reference/constellation/index.html

using System;
using System.Collections.Generic;
using Fritz.Chatbot.Commands;
using Fritz.StreamTools.Hubs;
using Fritz.StreamTools.Models;
using Microsoft.AspNetCore.SignalR;

namespace Fritz.StreamTools.Services
{
	public class FollowerClient
	{

		public FollowerClient(IHubContext<FollowerHub> followerContext)
		{

			this.FollowerContext = followerContext;

		}

		private IHubContext<FollowerHub> FollowerContext { get; }

		public void UpdateFollowers(int newFollowers)
		{

			FollowerContext.Clients.Group("followers").SendAsync("OnFollowersCountUpdated", newFollowers);

		}

		public void UpdateViewers(string serviceName, int viewerCount)
		{
			FollowerContext.Clients.Group("viewers").SendAsync("OnViewersCountUpdated", serviceName.ToLowerInvariant(), viewerCount);
		}

		internal void UpdateGitHub(IEnumerable<GitHubInformation> contributors)
		{
			FollowerContext.Clients.Group("github").SendAsync("OnGitHubUpdated", contributors);
		}

		public void UpdateCodeSuggestions(CodeSuggestion suggestion) {

			FollowerContext.Clients.Group("codesuggestions").SendAsync("OnNewCode", suggestion);

		}

	}

}
