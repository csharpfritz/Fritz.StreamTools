using Fritz.Chatbot.Commands;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Services
{
	public class SentimentService : IHostedService
	{

		private readonly FollowerClient _followerClient;
		private bool _StopProcess = false;
		private TextAnalyticsClient _client;
		private static string _SubscriptionKey;

		private class ApiKeyServiceClientCredentials : ServiceClientCredentials
		{
			public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				request.Headers.Add("Ocp-Apim-Subscription-Key", _SubscriptionKey);
				return base.ProcessHttpRequestAsync(request, cancellationToken);
			}
		}

		public SentimentService(FollowerClient followerClient, IConfiguration config)
		{

			_SubscriptionKey = config["FritzBot:SentimentAnalysisKey"].ToString();
			_followerClient = followerClient;
			_client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
			{
				Endpoint = "https://centralus.api.cognitive.microsoft.com"
			};
		}


		public Task StartAsync(CancellationToken cancellationToken)
		{
			Task.Run(async () => await Run());
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{

			_StopProcess = true;
			return Task.CompletedTask;

		}

		public async Task Run()
		{

			while (!_StopProcess)
			{

				if (SentimentSink.RecentChatMessages.Any())
				{

					var messageList = SentimentSink.RecentChatMessages.Select((value, index) => new MultiLanguageInput { Text = value, Id = index.ToString(), Language = "en" }).ToList();
					SentimentSink.RecentChatMessages.Clear();

					SentimentBatchResult results = await _client.SentimentAsync(
						new MultiLanguageBatchInput(messageList)
					);

					var avgScore = results.Documents
						.Where(d => d.Score.HasValue)
						.Average(d => d.Score).Value;
					_followerClient.UpdateSentiment(avgScore);

				}

				await Task.Delay(100);

			}

		}

	}
}
