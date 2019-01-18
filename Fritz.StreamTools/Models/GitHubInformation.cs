using System.Collections.Generic;

namespace Fritz.StreamTools.Models
{
  public class GitHubInformation
	{
		public GitHubInformation()
		{
			TopWeekContributors = new List<GitHubContributor>();
			TopMonthContributors = new List<GitHubContributor>();
			TopEverContributors = new List<GitHubContributor>();
		}

		public string Repository { get; set; }

		public List<GitHubContributor> TopWeekContributors { get; private set; }
		public List<GitHubContributor> TopMonthContributors { get; private set; }
		public List<GitHubContributor> TopEverContributors { get; private set; }
	}
}
