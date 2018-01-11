using System;
using Microsoft.Extensions.Configuration;

namespace Fritz.StreamTools.Services {

		public interface IStreamService {

				string Name { get; }

				int CurrentFollowerCount { get; }

				int CurrentViewerCount { get; }

				/// <summary>
				/// Event raised when the number of Followers or Viewers changes
				/// </summary>
				event EventHandler<ServiceUpdatedEventArgs> Updated;

		}

}