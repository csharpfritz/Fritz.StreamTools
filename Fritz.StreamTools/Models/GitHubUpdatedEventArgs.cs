using System;
using System.Collections.Generic;

namespace Fritz.StreamTools.Models
{
    public class GitHubUpdatedEventArgs : EventArgs
    {

			public GitHubUpdatedEventArgs(IEnumerable<GitHubInformation> newInformation, DateTime lastUpdate)
			{
					this.Contributors = newInformation;
					this.LastUpdate = lastUpdate;
			}

        public IEnumerable<GitHubInformation> Contributors { get; }
        public DateTime LastUpdate { get; }
    }

		public class GitHubNewContributorsEventArgs : EventArgs {

			public GitHubNewContributorsEventArgs(string repository, string userName, int newNumberOfCommits) {

			this.Repository = repository;
			this.UserName = userName;
			this.NewCommits = newNumberOfCommits;

			}

		public string Repository { get; }
		public string UserName { get; }
		public int NewCommits { get; }
	}


}
