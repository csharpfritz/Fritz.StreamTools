using Fritz.StreamTools.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Fritz.StreamTools.Controllers
{
  public class GitHubController : Controller
	{
		public GitHubController(
					IOptions<GitHubInformationConfiguration> githubConfiguration)
		{
			_gitHubConfiguration = githubConfiguration.Value;
		}

		private readonly GitHubInformationConfiguration _gitHubConfiguration;

		public IActionResult ContributorsInformation()
		{
			return View();
		}

		[Route("github/configuration", Name = "GitHubInformationConfiguration")]
		public IActionResult GitHubConfiguration()
		{
			return View();
		}
	}
}
