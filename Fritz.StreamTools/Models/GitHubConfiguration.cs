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

		public string ExcludeUser { get; set;} = "csharpfritz";

		public string RepositoryCsv { get; set; } = "csharpfritz/Fritz.StreamTools,csharpfritz/CoreWiki";

		public string DisplayMode {get;set;} = "h-scroll";

		public int Width { get; set; } = 600;

		public int SpeedInSec { get; set; } = 30;

		public string Font { get; set; } = "Arial";

		public int FontSizePt { get; set; } = 14;

		public bool CaptionBold { get; set; } = true;

		public string CaptionColor { get; set; } = "yellow";

		public string TextColor { get; set; } = "white";

		public string BackgroundColor { get; set; } = "#666";


  }
}
