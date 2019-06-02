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

	// Cheer 200 parithon 12/18/2018
	// Cheer 500 pharewings 12/18/2018
	Task AlertFritz();
	Task ClientConnected(string connectionId);
	Task SummonScott();

	Task PlaySoundEffect(string fileName);

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

	public Task SummonScott()
	{

	  return this.Clients.Others.SummonScott();

	}

	public Task PlaySoundEffect(string fileName)
	{

	  return this.Clients.Others.PlaySoundEffect(fileName);

	}

  }
}
