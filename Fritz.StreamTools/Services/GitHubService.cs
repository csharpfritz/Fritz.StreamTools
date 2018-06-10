using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Models;
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

		public event EventHandler<GitHubUpdatedEventArgs> Updated = null;

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

			while (!cancellationToken.IsCancellationRequested)
			{

				var lastUpdate = await GetLastCommittedTimestamp();
				if (lastUpdate > LastUpdate)
				{

					LastUpdate = lastUpdate;

					var newInfo = new GitHubInformation[] { };

					Logger.LogWarning($"Triggering refresh of GitHub scoreboard with updates as of {lastUpdate}");
					Updated?.Invoke(this, new GitHubUpdatedEventArgs(newInfo, lastUpdate));

				}
				await Task.Delay(500);

			}

		}

		private async Task<DateTime> GetLastCommittedTimestamp()
		{

			var repo = Services.GetService(typeof(GitHubRepository)) as GitHubRepository;

			return await repo.GetLastCommitTimestamp();


		}

	}
}
