using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Fritz.StreamTools.Hubs
{

	public abstract class BaseHub : Hub {

		public override async Task OnConnectedAsync()
		{
			var groupNames = Context.GetHttpContext().Request.Query["groups"].SingleOrDefault();
			if (groupNames != null)
			{
				// Join the group(s) the user has specified in the 'groups' query-string
				// NOTE: SignalR will automatically take care of removing the client from the group(s) when they disconnect
				foreach (var groupName in groupNames.Split(','))
					await Groups.AddToGroupAsync(Context.ConnectionId, groupName.ToLowerInvariant());
			}

			await base.OnConnectedAsync();
		}



	}

}
