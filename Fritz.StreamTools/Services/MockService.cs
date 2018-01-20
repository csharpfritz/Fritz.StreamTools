using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Services
{
	public class MockService : IHostedService, IStreamService
	{

		IConfiguration _config;

		int _numberOfFollowers;
		int _numberOfViewers;
		int _updateViewersInterval;
		int _updateFollowersInterval;
		Timer _updateViewers;
		Timer _updateFollowers;

		public string Name => "Mock";

		public ILogger Logger { get; }

		public MockService(IConfiguration config, ILoggerFactory loggerFactory)
		{

			this._config = config;
			this.Logger = loggerFactory.CreateLogger("StreamServices");

		}

		public int CurrentFollowerCount => _numberOfFollowers;

		public int CurrentViewerCount => _numberOfViewers;

		public event EventHandler<ServiceUpdatedEventArgs> Updated;

		public Task StartAsync(CancellationToken cancellationToken)
		{

			_numberOfFollowers = int.Parse("0" + _config["StreamServices:Mock:CurrentFollowerCount"]);
			_numberOfViewers = int.Parse("0" + _config["StreamServices:Mock:CurrentViewerCount"]);
			_updateViewersInterval = int.Parse("0" + _config["StreamServices:Mock:UpdateViewersInterval"]);
			_updateFollowersInterval = int.Parse("0" + _config["StreamServices:Mock:UpdateFollowersInterval"]);

			Logger.LogInformation($"Now monitoring Mock with {CurrentFollowerCount} followers and {CurrentViewerCount} Viewers");

			SetupViewerUpdateTimer();
			SetupFollowerUpdateTimer();

			return Task.CompletedTask;

		}

		private void SetupFollowerUpdateTimer()
		{
			if (_updateFollowersInterval == default(int))
			{
				return;
			}

			_updateFollowers = new Timer((o) =>
			{

				if (_numberOfFollowers >= 100)
				{
					_numberOfFollowers = 1;
				}

				_numberOfFollowers++;

				Logger.LogInformation($"New Followers on Mock, new total: {_numberOfFollowers}");

				Updated?.Invoke(
					null,
					new ServiceUpdatedEventArgs()
					{
						NewFollowers = _numberOfFollowers,
						ServiceName = Name
					});

			},
			null,
			4000,
			_updateFollowersInterval);

		}

		private void SetupViewerUpdateTimer()
		{
			if (_updateViewersInterval == default(int))
			{
				return;
			}

			_updateViewers = new Timer((o) =>
			{

				if (_numberOfViewers >= 100)
				{
					_numberOfViewers = 1;
				}

				_numberOfViewers++;

				Logger.LogInformation($"New Followers on Mock, new total: {_numberOfViewers}");

				Updated?.Invoke(
					null,
					new ServiceUpdatedEventArgs()
					{
						NewViewers = _numberOfViewers,
						ServiceName = Name
					});

			},
			null,
			5000,
			_updateViewersInterval);

		}

		public Task StopAsync(CancellationToken cancellationToken)
		{

			Logger.LogInformation($"Stopping monitoring Mock with {CurrentFollowerCount} followers and {CurrentViewerCount} Viewers");

			if (_updateViewers != null)
			{
				_updateViewers.Dispose();
			}

			if (_updateFollowers != null)
			{
				_updateFollowers.Dispose();
			}

			return Task.CompletedTask;

		}
	}
}