using Fritz.StreamTools.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Octokit;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;

namespace Fritz.StreamTools.Controllers
{
  public class GitHubController : Controller
	{
		public GitHubController(
			IMemoryCache cache,
			GitHubClient githubClient,
			IOptions<GitHubConfiguration> githubConfiguration)
		{
			this.Cache = cache;
			_gitHubClient = githubClient;
			_gitHubConfiguration = githubConfiguration.Value;
		}

    public IMemoryCache Cache { get; }

    private readonly GitHubClient _gitHubClient;
		private readonly GitHubConfiguration _gitHubConfiguration;

		public async Task<IActionResult> ContributorsInformation()
		{
			var model = new GitHubInformation();

			model = await Cache.GetOrCreateAsync<GitHubInformation>("GitHubData", async (x) => {

				x.AbsoluteExpiration = DateTime.Now.AddMinutes(5);

				var repository =
					await _gitHubClient.Repository.Get(_gitHubConfiguration.RepositoryOwner, _gitHubConfiguration.RepositoryName);
				var contributors =
					await _gitHubClient.Repository.Statistics.GetContributors(repository.Id);
				var lastMonth = DateTimeOffset.Now.AddMonths(-1);

				model.TopEverContributors.AddRange(
								contributors.Where(c => c.Total > 0)
														.OrderByDescending(c => c.Total)
														.Take(5)
														.Select(c => new GitHubContributor() {
																					Author = c.Author.Login,
																					Commits = c.Total
														}));

				model.TopMonthContributors.AddRange(
								contributors.OrderByDescending(c => c.Weeks.Where(w => w.Week >= lastMonth)
																														.Sum(e => e.Commits))
														.Select(c => new GitHubContributor {
																					Author = c.Author.Login,
																					Commits = c.Weeks.Where(w => w.Week >= lastMonth)
																														.Sum(e => e.Commits)
														})
														.Where(c => c.Commits > 0)
														.OrderByDescending(c => c.Commits)
														.Take(5));

				model.TopWeekContributors.AddRange(
								contributors.Where(c => c.Weeks.Last().Commits > 0)
														.Select(c => new GitHubContributor {
																								Author = c.Author.Login,
																								Commits = c.Weeks.Last().Commits
														})
														.Where(c => c.Commits > 0)
														.OrderByDescending(c => c.Commits)
														.Take(5));

				return model;

			});

			return View(model);

		}

		public IActionResult Configuration()
		{
			return View(_gitHubConfiguration);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Configuration(GitHubConfiguration configuration)
		{
			if (ModelState.IsValid)
			{
					_gitHubConfiguration.RepositoryName = configuration.RepositoryName;
					_gitHubConfiguration.RepositoryOwner = configuration.RepositoryOwner;
			}

			return View(configuration);
		}

		[AcceptVerbs("Get", "Post")]
		public async Task<IActionResult> VerifyUser(string repositoryOwner)
		{
			try
			{
				var user
					= await _gitHubClient.User.Get(repositoryOwner);
			}
			catch (NotFoundException)
			{
				return Json("User not found.");
			}
			catch (Exception)
			{
				return Json("Ops... something went wrong.");
			}

			return Json(true);
		}

		[AcceptVerbs("Get", "Post")]
		public async Task<IActionResult> VerifyRepository(string repositoryName, string repositoryOwner)
		{
			try
			{
				var repository =
					await _gitHubClient.Repository.Get(repositoryOwner, repositoryName);
			}
			catch (NotFoundException)
			{
				return Json("Repository not found.");
			}
			catch (Exception)
			{
				return Json("Ops... something went wrong.");
			}

			return Json(true);
		}
  }
}
