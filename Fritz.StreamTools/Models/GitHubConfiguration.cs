using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Fritz.StreamTools.Models
{
  public class GitHubConfiguration
  {
		[Required]
		[Display(Name = "Repository Owner")]
		[Remote(action: "VerifyUser", controller: "GitHub")]
		public string RepositoryOwner { get; set; }

		[Required]
		[Display(Name = "Repository Name")]
		[Remote(action: "VerifyRepository", controller: "GitHub", AdditionalFields = nameof(RepositoryOwner))]
		public string RepositoryName { get; set; }

		public string DisplayMode {get;set;} = "h-scroll";

  }
}
