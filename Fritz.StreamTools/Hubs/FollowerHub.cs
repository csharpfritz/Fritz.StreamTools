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
using Microsoft.Extensions.Logging;

namespace Fritz.StreamTools.Hubs
{

	public class FollowerHub : BaseHub
	{
		//private readonly CodeSuggestionsManager CodeSuggestionsManager;

		public StreamService StreamService { get; }
		public FollowerClient FollowerClient { get; }
		public ILogger Logger { get; }

		public FollowerHub(
			StreamService streamService,
			FollowerClient client,
			ILoggerFactory loggerFactory
			)
		{


			this.StreamService = streamService;
			this.FollowerClient = client;
			this.Logger = loggerFactory.CreateLogger("SignalR");

			CodeSuggestionsManager.Instance.SuggestionAdded = (suggestion) => this.FollowerClient.UpdateCodeSuggestions(suggestion);

			StreamService.Updated += StreamService_Updated;
		}

		public Task WhisperToUser(string userName, string message) {

			/*
			 *Cheer 219 Magnus10 10:14 1/3/2019
				Cheer 218 SqlMisterMagoo 10:52 1/4/2019
			*/

			FritzBot.WhisperQueue.Add((userName, message));

			return Task.CompletedTask;

		}

		public override Task OnConnectedAsync()
		{

			Logger.LogError($"New connection from: {this.Context.ConnectionId}");

			return base.OnConnectedAsync();
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
