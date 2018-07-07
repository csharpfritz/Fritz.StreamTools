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

		private (string user, string repo)[] _Repositories = null;

		private async Task<List<GitHubInformation>> FetchContributersFromGithub(string repositoryCsv = "")
		{
			var outModel = new List<GitHubInformation>();
			Logger.LogWarning("Fetching data from GitHub");

		var repositories = GetRepositories(repositoryCsv);
		var lastMonth = DateTimeOffset.Now.AddMonths(-1);

		foreach (var (user, repo) in repositories)
	  {
			var model = new GitHubInformation() { Repository = repo };
			IReadOnlyList<Contributor> contributors;
			try
			{
				contributors =
			  await Client.Repository.Statistics.GetContributors(user, repo);
			}
			catch (RateLimitExceededException)
			{
			// do nothing... return empty collection
				return outModel;
			}
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
	}

		public async Task<IEnumerable<GitHubInformation>> GetRecentContributors(string repositoryCsv = "")
		{
			return await AppCache.GetOrAddAsync<List<GitHubInformation>>("GitHubData", x =>
			{
				x.AbsoluteExpiration = DateTime.Now.AddMinutes(60);
				return FetchContributersFromGithub(repositoryCsv);
			});
		}

		public static DateTime LastUpdate = DateTime.MinValue;

		public async Task<(DateTime, string, string)> GetLastCommitTimestamp(string repositoryCsv = "") {

			return await AppCache.GetOrAddAsync("GitHubLastCommit", async x =>
			{

				x.AbsoluteExpiration = DateTime.UtcNow.AddMinutes(1);

				var thisLastUpdate = DateTime.MinValue;

				foreach (var r in GetRepositories(repositoryCsv))
				{

					Logger.LogInformation($"Getting GitHub last update information for {r}");

					var updateInfo = (await Client.Repository.Get(r.user, r.repo));
					Logger.LogInformation($"{r} last updated at: {updateInfo.UpdatedAt.UtcDateTime}");

					thisLastUpdate = (thisLastUpdate < updateInfo.UpdatedAt.UtcDateTime) ? updateInfo.UpdatedAt.UtcDateTime : thisLastUpdate;

					// updatedRepository = r;

				}

				if (LastUpdate < thisLastUpdate)
				{
					AppCache.Remove("GitHubData");
				}

				return (thisLastUpdate, "", "");

			});

		}

		private (string user, string repo)[] GetRepositories(string repositoryCsv = "")
		{

			if (_Repositories != null)
			{
				return _Repositories;
			}

			if (string.IsNullOrEmpty(repositoryCsv))
			{
				repositoryCsv = Configuration.RepositoryCsv;
			}

			var arrRepositories = repositoryCsv.Split(',');
			var outArr = new(string, string)[] { };

			foreach (var repo in arrRepositories)
			{

				var parts = repo.Split('/');
				outArr = outArr.Append((parts[0], parts[1])).ToArray();

			}

			_Repositories = outArr;
			return outArr;

		}
	}

}
