using Fritz.StreamTools.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Octokit;
using System.Threading.Tasks;
using System.Linq;
using System;
using LazyCache;
using Microsoft.Extensions.Logging;
using Fritz.StreamTools.Services;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;

namespace Fritz.StreamTools.Controllers
{
	public class GitHubController : Controller
	{
		public GitHubController(
			IAppCache cache,
			GitHubRepository repository,
			GithubyMcGithubFaceClient client,
			ILogger<GitHubController> logger,
			IOptions<GitHubConfiguration> githubConfiguration,
			IHttpClientFactory httpClientFactory)
		{
			this.Cache = cache;
			this.Logger = logger;
			this.Client = client;
			_gitHubRepository = repository;
			_gitHubConfiguration = githubConfiguration.Value;
			_httpClient = httpClientFactory.CreateClient("DiscoverDotNet");
		}

		public IAppCache Cache { get; }
		public ILogger<GitHubController> Logger { get; }
		public GithubyMcGithubFaceClient Client { get; }

		private readonly GitHubRepository _gitHubRepository;

		private readonly GitHubConfiguration _gitHubConfiguration;

		private readonly HttpClient _httpClient;

		private static JsonSerializer _serializer = new JsonSerializer();

		public async Task<IActionResult> ContributorsInformation(string repo, string userName, int count)
		{
			var outModel = await _gitHubRepository.GetRecentContributors(_gitHubConfiguration.RepositoryCsv);

			if (!string.IsNullOrEmpty(repo))
			{
				outModel.First(i => i.Repository.Equals(repo, StringComparison.InvariantCultureIgnoreCase))
				.TopWeekContributors.Add(new GitHubContributor
				{
					Author = userName,
					Commits = count
				});
			}

			var newsPosts = await GetNewsFromDiscoverDotNet();
			var outViewModel = new TickerViewModel()
			{
				GitHubInformation = outModel.ToArray(),
				News = newsPosts
			};

			ViewBag.Configuration = _gitHubConfiguration;

			return View($"contributor_{_gitHubConfiguration.DisplayMode}", outViewModel);

		}

		private async Task<(string source, string color, IEnumerable<BlogPostModel> blogPosts)> GetNewsFromDiscoverDotNet()
		{
			IList<BlogPostModel> posts;
			using(Stream response = await _httpClient.GetStreamAsync("https://discoverdot.net/data/news.json"))
			{
				using(StreamReader reader = new StreamReader(response))
				{
					using(JsonTextReader json = new JsonTextReader(reader))
					{
						posts = _serializer.Deserialize<IList<BlogPostModel>>(json);
					}
				}
			}		
			return ("Discover .NET", "#551A8B", posts.Take(5).ToArray());
		}

		public IActionResult Configuration()
		{
			return View(_gitHubConfiguration);
		}

		[HttpGet("api/GitHub/Latest")]
		public async Task<IActionResult> LatestChanges()
		{

			var outModel = await _gitHubRepository.GetLastCommitTimestamp(_gitHubConfiguration.RepositoryCsv);

			return Ok(outModel.Item1.ToString("MM/dd/yyyy HH:mm:ss"));

		}

		[HttpGet("api/GitHub/Contributors")]
		public async Task<IActionResult> GetContributors()
		{

			var outModel = await _gitHubRepository.GetRecentContributors(_gitHubConfiguration.RepositoryCsv);

			return Ok(outModel);

		}


		public IActionResult Test(int value, string devName, string projectName) {

			GitHubService.LastUpdate = DateTime.MinValue;

			return Json(0);


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
					= await _gitHubRepository.Client.User.Get(repositoryOwner);
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
					await _gitHubRepository.Client.Repository.Get(repositoryOwner, repositoryName);
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

	public class TickerViewModel {

		public IEnumerable<GitHubInformation> GitHubInformation { get; set; }

		public (string source, string color, IEnumerable<BlogPostModel> blogPosts) News { get; set; }

	}

	public class BlogPostModel
	{
		public string Title { get; set; }
		public string FeedTitle { get; set; }
		public string Link { get; set; }
		public DateTime Published { get; set; }
	}

}
