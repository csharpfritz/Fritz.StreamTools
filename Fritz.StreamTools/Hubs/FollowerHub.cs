using System.Net.Mime;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.StreamTools.Models;
using Fritz.Chatbot.Commands;

namespace Fritz.StreamTools.Hubs
{

	public class FollowerHub : BaseHub
	{
		//private readonly CodeSuggestionsManager CodeSuggestionsManager;

		public StreamService StreamService { get; }
		public FollowerClient FollowerClient { get; }

		public FollowerHub(
			StreamService streamService,
			FollowerClient client
			)
		{


			this.StreamService = streamService;
			this.FollowerClient = client;

			CodeSuggestionsManager.Instance.SuggestionAdded = (suggestion) => this.FollowerClient.UpdateCodeSuggestions(suggestion);

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
