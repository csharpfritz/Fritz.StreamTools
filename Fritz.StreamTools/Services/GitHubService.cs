using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fritz.StreamTools.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Fritz.StreamTools.Services
{
	public class GitHubService : IHostedService
	{

		private DateTime _LastUpdate = DateTime.MinValue;

		public GitHubService(IServiceProvider services)
		{
			this.Services = services;
		}

		public IServiceProvider Services { get; }

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
				if (lastUpdate > this._LastUpdate)
				{

					_LastUpdate = lastUpdate;

					var newInfo = new GitHubInformation[] { };

					if (Updated != null) Updated.Invoke(this, new GitHubUpdatedEventArgs(newInfo, lastUpdate));

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
