using Fritz.StreamLib.Core;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Hubs
{
	public interface IAttentionHubClient
	{
		Task AlertFritz();
		Task ClientConnected(string connectionId);
	}

	public class AttentionHub : Hub<IAttentionHubClient>, IAttentionClient
	{
		public override Task OnConnectedAsync()
		{
			return this.Clients.Others.ClientConnected(this.Context.ConnectionId);
		}

		public Task AlertFritz()
		{
			return this.Clients.Others.AlertFritz();
		}
	}
}
