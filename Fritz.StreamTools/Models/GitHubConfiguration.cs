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

		public List<GitHubContributor> TopWeekContributors { get; private set; }
		public List<GitHubContributor> TopMonthContributors { get; private set; }
		public List<GitHubContributor> TopEverContributors { get; private set; }
	}

	public class GitHubInformationConfiguration
  {
		public string RepositoryName { get; private set; }
		public string RepositoryOwner { get; private set; }

		internal void LoadDefaultSettings(GitHubInformationConfiguration configuration)
		{
			RepositoryName = configuration.RepositoryName;
			RepositoryOwner = configuration.RepositoryOwner;
		}
  }

	public class GitHubContributor
  {
		public string Author { get; set; }
  }
}
