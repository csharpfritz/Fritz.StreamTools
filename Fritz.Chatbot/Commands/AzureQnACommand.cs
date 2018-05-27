using System;
using System.Collections.Generic;
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

	public class AzureQnACommand : ICommand
	{

		public IConfiguration Configuration { get; set; }

		public ILogger Logger { get; set; }

		public string Name => "";

		public string AzureKey => Configuration["AzureServices:QnASubscriptionKey"];

		public string KnowledgebaseId => Configuration["FritzBot:QnAKnowledgeBaseId"];

		public string Description => "Answer questions using Azure Cognitive Services and Jeff's FAQ on the LiveStream wiki";

		public int Order => 1;

		public bool CanExecute (string userName, string fullCommandText)
		{

			return fullCommandText.EndsWith ("?");

		}

		public async Task Execute (IChatService chatService, string userName, string fullCommandText)
		{

			// Exit now if we don't know how to connect to Azure
			if (string.IsNullOrEmpty (AzureKey)) return;

			Logger.LogInformation ($"Handling question: \"{fullCommandText}\" from {userName}");

			await Query (chatService, userName, fullCommandText);

		}

		public async Task Query (IChatService chatService, string userName, string query)
		{

			var responseString = string.Empty;
			query = WebUtility.UrlEncode (query);

			//Build the URI
			var qnamakerUriBase = new Uri ("https://westus.api.cognitive.microsoft.com/qnamaker/v1.0");
			var builder = new UriBuilder ($"{qnamakerUriBase}/knowledgebases/{KnowledgebaseId}/generateAnswer");

			//Add the question as part of the body
			var postBody = $"{{\"question\": \"{query}\"}}";

			//Send the POST request
			using (var client = new WebClient ())
			{
				//Set the encoding to UTF8
				client.Encoding = System.Text.Encoding.UTF8;

				//Add the subscription key header
				client.Headers.Add ("Ocp-Apim-Subscription-Key", AzureKey);
				client.Headers.Add ("Content-Type", "application/json");

				try
				{
					responseString = await client.UploadStringTaskAsync (builder.Uri, postBody).OrTimeout ();
				}
				catch (TimeoutException)
				{
					Logger.LogWarning ($"Azure Services did not respond in time to question '{query}'");
					chatService.SendMessageAsync ($"Unable to answer the question '{query}' at this time").Forget ();
					return;
				}
			}

			QnAMakerResult response;
			try
			{
				response = JsonConvert.DeserializeObject<QnAMakerResult> (responseString);

				response.Answer = WebUtility.HtmlDecode (response.Answer).HandleMarkdownLinks ();

				if (response.Score > 50)
				{
					await chatService.SendMessageAsync (response.Answer);
				}
				else if (response.Score > 30)
				{
					await chatService.SendMessageAsync ("I'm not certain, but perhaps this will help:  " + response.Answer + $@"({response.Score.ToString("0.0")}% certainty)");

				}
				else
				{
					Logger.LogInformation ($"Unable to find suitable answer to {userName}'s question: {query}");
				}

			}
			catch (Exception ex) when (Logger.LogAndSwallow ("asking knowledgebase", ex))
			{

			}

		}

		public async Task Retrain ()
		{

			var qnamakerUriBase = new Uri ("https://westus.api.cognitive.microsoft.com/qnamaker/v2.0");
			var builder = new UriBuilder ($"{qnamakerUriBase}/knowledgebases/{KnowledgebaseId}");

			//Send the POST request
			using (var client = new WebClient ())
			{
				//Set the encoding to UTF8
				client.Encoding = System.Text.Encoding.UTF8;

				//Add the subscription key header
				client.Headers.Add ("Ocp-Apim-Subscription-Key", AzureKey);
				client.Headers.Add ("Content-Type", "application/json");

				//Add the question as part of the body
				var postBody = $"{{\"add\": {{\"urls\": [\"https://github.com/csharpfritz/Fritz.LiveStream/wiki/Frequently-Asked-Questions\"]}} }}";

				var responseString = await client.UploadStringTaskAsync (builder.Uri, "PATCH", postBody);
			}

		}

	}

}
