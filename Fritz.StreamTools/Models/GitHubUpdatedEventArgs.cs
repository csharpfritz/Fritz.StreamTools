using Fritz.Models;
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
}
