using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fritz.Models;
using Fritz.StreamTools.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Fritz.StreamTools.Services
{
	public class GitHubService : IHostedService
	{

		private DateTime _LastUpdate = DateTime.MinValue;

		public GitHubService(IHttpClientFactory httpClientFactory)
		{
			this.HttpClient = httpClientFactory.CreateClient("GitHub");
		}

		public HttpClient HttpClient { get; }

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

				if (lastRequest.AddMinutes(1) > DateTime.Now) continue;

				lastRequest = DateTime.Now;

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

			var result = await this.HttpClient.GetAsync("https://localhost:5001/api/GitHub/Latest");

			// "2018-06-02T15:08:38Z"
			var resultDate = await result.Content.ReadAsStringAsync();

			return DateTime.ParseExact(resultDate, "MM/dd/yyyy HH:mm:ss", null);


		}

	}
}
