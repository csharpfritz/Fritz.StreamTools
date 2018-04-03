using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Fritz.Chatbot.Commands
{

	public class AzureQnACommand : ICommand
	{

		public IConfiguration Configuration { get; set; }

		public IChatService ChatService { get; set; }

		public string Name => "qna";

		public string AzureKey => Configuration["AzureServices:QnASubscriptionKey"];

		public string KnowledgebaseId => Configuration["FritzBot:QnAKnowledgeBaseId"];

		public string Description => "Answer questions using Azure Cognitive Services and Jeff's FAQ on the LiveStream wiki";

		public async Task Execute(string userName, string fullCommandText)
		{

			// Exit now if we don't know how to connect to Azure
			if (string.IsNullOrEmpty(AzureKey)) return;
 
			await Query(fullCommandText);
			
		}

		public async Task Query(string query)
		{

			var responseString = string.Empty;
			query = WebUtility.UrlEncode(query);

			//Build the URI
			var qnamakerUriBase = new Uri("https://westus.api.cognitive.microsoft.com/qnamaker/v1.0");
			var builder = new UriBuilder($"{qnamakerUriBase}/knowledgebases/{KnowledgebaseId}/generateAnswer");

			//Add the question as part of the body
			var postBody = $"{{\"question\": \"{query}\"}}";

			//Send the POST request
			using (var client = new WebClient())
			{
				//Set the encoding to UTF8
				client.Encoding = System.Text.Encoding.UTF8;

				//Add the subscription key header
				client.Headers.Add("Ocp-Apim-Subscription-Key", AzureKey);
				client.Headers.Add("Content-Type", "application/json");
				responseString = client.UploadString(builder.Uri, postBody);
			}

			QnAMakerResult response;
			try
			{
				response = JsonConvert.DeserializeObject<QnAMakerResult>(responseString);

				response.Answer = WebUtility.HtmlDecode(response.Answer);

				if (response.Score > 50)
				{
					await ChatService.SendMessageAsync(response.Answer);
				} else if (response.Score > 30)
				{
					await ChatService.SendMessageAsync("I'm not certain, but prehaps this will help:  " + response.Answer + $@"({response.Score.ToString("0.0")}% certainty)");

				}

			}
			catch
			{
				// Not sure what to do with this yet...

				//throw new Exception("Unable to deserialize QnA Maker response string.");
			}

		}

	}

}
