using Fritz.StreamLib.Core;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Hubs
{

	public class FollowerHub : Hub
	{
		public StreamService StreamService { get; }
		public FollowerClient FollowerClient { get; }

		public FollowerHub(
			StreamService streamService,
			FollowerClient client
			)
		{

			this.StreamService = streamService;
			this.FollowerClient = client;

			StreamService.Updated += StreamService_Updated;
		}

		private void StreamService_Updated(object sender, ServiceUpdatedEventArgs e)
		{
			if (e.NewFollowers.HasValue)
			{
				this.FollowerClient.UpdateFollowers(StreamService.CurrentFollowerCount);
			}

			if (e.NewViewers.HasValue)
			{
				this.FollowerClient.UpdateViewers(e.ServiceName, e.NewViewers.Value);
			}
		}
	}

}
