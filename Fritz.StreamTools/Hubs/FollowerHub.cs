using System.Net.Mime;
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

		public override async Task OnConnectedAsync()
		{
			var groupNames = Context.GetHttpContext().Request.Query["groups"].SingleOrDefault();
			if (groupNames != null)
			{
				// Join the group(s) the user has specified in the 'groups' query-string
				// NOTE: SignalR will automatically take care of removing the client from the group(s) when they disconnect
				foreach (var groupName in groupNames.Split(','))
					await Groups.AddAsync(Context.ConnectionId, groupName.ToLowerInvariant());
			}

			await base.OnConnectedAsync();
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
