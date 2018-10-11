using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Fritz.Chatbot.Helpers;
using Fritz.ChatBot.Helpers;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fritz.Chatbot.Commands
{

	public class AzureQnACommand : IExtendedCommand
	{
		public string AzureKey => _configuration["AzureServices:QnASubscriptionKey"];
		public string KnowledgebaseId => _configuration["FritzBot:QnAKnowledgeBaseId"];

		public string Name => "AzureQnA";
		public string Description => "Answer questions using Azure Cognitive Services and Jeff's FAQ on the LiveStream wiki";
		public int Order => 1;
		public bool Final => true;
		public TimeSpan? Cooldown => null;

		private readonly IConfiguration _configuration;
		private readonly ILogger<AzureQnACommand> _logger;

		public AzureQnACommand(IConfiguration configuration, ILogger<AzureQnACommand> logger)
		{
			_configuration = configuration;
			_logger = logger;
		}

		public bool CanExecute(string userName, string fullCommandText)
		{
			return fullCommandText.EndsWith("?");
		}

		public async Task Execute(IChatService chatService, string userName, string fullCommandText)
		{
			// Exit now if we don't know how to connect to Azure
			if (string.IsNullOrEmpty(AzureKey)) return;

			_logger.LogInformation($"Handling question: \"{fullCommandText}\" from {userName}");

			await Query(chatService, userName, fullCommandText);
		}

		public async Task Query(IChatService chatService, string userName, string query)
		{
			var responseString = string.Empty;
			query = WebUtility.UrlEncode(query);

			//Build the URI
			var qnamakerUriBase = new Uri("https://fritzbotqna.azurewebsites.net/qnamaker");
			var builder = new UriBuilder($"{qnamakerUriBase}/knowledgebases/{KnowledgebaseId}/generateAnswer");

			//Add the question as part of the body
			var postBody = $"{{\"question\": \"{query}\"}}";

			//Send the POST request
			using(var client = new WebClient())
			{
				//Set the encoding to UTF8
				client.Encoding = System.Text.Encoding.UTF8;

				//Add the subscription key header
				client.Headers.Add("Authorization", $"EndpointKey {AzureKey}");
				client.Headers.Add("Content-Type", "application/json");
				// client.Headers.Add("Host", "https://fritzbotqna.azurewebsites.net/qnamaker");

				try
				{
					responseString = await client.UploadStringTaskAsync(builder.Uri, postBody).OrTimeout();
				}
				catch (TimeoutException)
				{
					_logger.LogWarning($"Azure Services did not respond in time to question '{query}'");
					chatService.SendMessageAsync($"Unable to answer the question '{query}' at this time").Forget();
					return;
				}
                catch(Exception ex)
                {
                    _logger.LogError($">>> Error while communicating with QnA service: {ex.ToString()}");
										return;
                }
			}

			QnAMakerResult response;
			try
			{
				response = JsonConvert.DeserializeObject<QnAMakerResult>(responseString);

				var thisAnswer = response.Answers.OrderByDescending(a => a.Score).FirstOrDefault();

				thisAnswer.Answer = WebUtility.HtmlDecode(thisAnswer.Answer).HandleMarkdownLinks();


				if (thisAnswer.Score > 50)
				{
					await chatService.SendMessageAsync(thisAnswer.Answer);
				}
				else if (thisAnswer.Score > 30)
				{
					await chatService.SendMessageAsync("I'm not certain, but perhaps this will help:  " + thisAnswer.Answer + $@"({thisAnswer.Score.ToString("0.0")}% certainty)");

				}
				else
				{
					_logger.LogInformation($"Unable to find suitable answer to {userName}'s question: {query}");
				}

			}
			catch (Exception ex) when(_logger.LogAndSwallow("asking knowledgebase", ex))
			{

			}
		}

		public async Task Retrain()
		{
			var qnamakerUriBase = new Uri("https://westus.api.cognitive.microsoft.com/qnamaker/v2.0");
			var builder = new UriBuilder($"{qnamakerUriBase}/knowledgebases/{KnowledgebaseId}");

			//Send the POST request
			using(var client = new WebClient())
			{
				//Set the encoding to UTF8
				client.Encoding = System.Text.Encoding.UTF8;

				//Add the subscription key header
				client.Headers.Add("Ocp-Apim-Subscription-Key", AzureKey);
				client.Headers.Add("Content-Type", "application/json");

				//Add the question as part of the body
				var postBody = $"{{\"add\": {{\"urls\": [\"https://github.com/csharpfritz/Fritz.LiveStream/wiki/Frequently-Asked-Questions\"]}} }}";

				var responseString = await client.UploadStringTaskAsync(builder.Uri, "PATCH", postBody);
			}
		}
	}
}
