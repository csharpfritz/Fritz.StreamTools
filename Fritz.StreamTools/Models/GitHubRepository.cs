using LazyCache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Models
{

	public class GitHubRepository
	{

		public GitHubRepository(GitHubClient client, IOptions<GitHubConfiguration> config,
		IAppCache appCache,
		ILogger<GitHubRepository> logger)
		{
			this.AppCache = appCache;
			this.Configuration = config.Value;
			this.Logger = logger;
			this.Client = client;
		}

		public IAppCache AppCache { get; }
		public GitHubConfiguration Configuration { get; }
		public ILogger<GitHubRepository> Logger { get; }

		public readonly GitHubClient Client;

		public async Task<IEnumerable<GitHubInformation>> GetRecentContributors(string repositoryCsv) {

			var outModel = new List<GitHubInformation>();

			return await AppCache.GetOrAddAsync<List<GitHubInformation>>("GitHubData", async (x) => {

				x.AbsoluteExpiration = DateTime.Now.AddSeconds(45);

				Logger.LogWarning("Fetching data from GitHub");

				var repositories = repositoryCsv.Split(',');

				foreach (var repo in repositories)
				{

					var thisRepo = repo.Split('/')[1];
					var thisUser = repo.Split('/')[0];
					var model = new GitHubInformation() { Repository = thisRepo };

					var repository =
						await Client.Repository.Get(thisUser, thisRepo);
					var contributors =
						await Client.Repository.Statistics.GetContributors(repository.Id);
					var lastMonth = DateTimeOffset.Now.AddMonths(-1);

					model.TopEverContributors.AddRange(
									contributors.Where(c => c.Total > 0 && c.Author.Login != Configuration.ExcludeUser)
															.OrderByDescending(c => c.Total)
															.Take(5)
															.Select(c => new GitHubContributor()
															{
																Author = c.Author.Login,
																Commits = c.Total
															}));

					model.TopMonthContributors.AddRange(
									contributors.OrderByDescending(c => c.Weeks.Where(w => w.Week >= lastMonth)
																															.Sum(e => e.Commits))
															.Select(c => new GitHubContributor
															{
																Author = c.Author.Login,
																Commits = c.Weeks.Where(w => w.Week >= lastMonth)
																															.Sum(e => e.Commits)
															})
															.Where(c => c.Commits > 0 && c.Author != Configuration.ExcludeUser)
															.OrderByDescending(c => c.Commits)
															.Take(5));

					model.TopWeekContributors.AddRange(
									contributors.Where(c => c.Weeks.Last().Commits > 0)
															.Select(c => new GitHubContributor
															{
																Author = c.Author.Login,
																Commits = c.Weeks.Last().Commits
															})
															.Where(c => c.Commits > 0 && c.Author != Configuration.ExcludeUser)
															.OrderByDescending(c => c.Commits)
															.Take(5));

					outModel.Add(model);

				}

				return outModel;

			});

		}

		public async Task<DateTime> GetLastCommitTimestamp(string repositoryCsv) {

			var lastUpdates = new DateTime[] {};

			var repositories = repositoryCsv.Split(',');
			foreach (var r in repositories) {

				var userName = r.Split('/')[0];
				var repoName = r.Split('/')[1];

				lastUpdates.Append((await Client.Repository.Get(userName, repoName)).UpdatedAt.DateTime);

			}

			return lastUpdates.Max();

		}

	}

}
