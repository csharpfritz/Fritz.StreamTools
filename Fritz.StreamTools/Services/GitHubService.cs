using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fritz.StreamTools.Services
{
	public class GitHubService : IHostedService
	{

		public static DateTime LastUpdate = DateTime.MinValue;

		public GitHubService(IServiceProvider services, ILogger<GitHubService> logger)
		{
			this.Services = services;
			this.Logger = logger;
		}

		public IServiceProvider Services { get; }
		public ILogger<GitHubService> Logger { get; }

		public Task StartAsync(CancellationToken cancellationToken)
		{
			return MonitorUpdates(cancellationToken);
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		private async Task MonitorUpdates(CancellationToken cancellationToken)
		{
			var lastRequest = DateTime.Now;
		  using (var scope = Services.CreateScope())
		  {
			  var repo = scope.ServiceProvider.GetService(typeof(GitHubRepository)) as GitHubRepository;
			  var mcGithubFaceClient = scope.ServiceProvider.GetService(typeof(GithubyMcGithubFaceClient)) as GithubyMcGithubFaceClient;
				while (!cancellationToken.IsCancellationRequested)
			  {
				  if (repo != null)
				  {
					  var lastUpdate = await repo.GetLastCommitTimestamp();
					  if (lastUpdate.Item1 > LastUpdate)
					  {

						  LastUpdate = lastUpdate.Item1;

						  Logger.LogWarning($"Triggering refresh of GitHub scoreboard with updates as of {lastUpdate}");
						  mcGithubFaceClient?.UpdateGitHub("", "", 0);
					  }
				  }
				  await Task.Delay(500, cancellationToken);
			  }
		  }
		}
	}
}
