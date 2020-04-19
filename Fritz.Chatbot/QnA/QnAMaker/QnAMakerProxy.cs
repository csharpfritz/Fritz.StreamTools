using Fritz.Chatbot.Commands;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fritz.Chatbot.QnA.QnAMaker
{

	public class Proxy
	{

		private readonly string _KnowledgeBaseId;
		private readonly bool _MissingAzureKey;
		private readonly IHttpClientFactory _HttpClientFactory;
		public const string MaintenanceClientName = "QnAMaintainanace";
		public const string QuestionClientName = "QnAQuestion";


		public Proxy(IHttpClientFactory httpClientFactory, IConfiguration configuration)
		{
			this._KnowledgeBaseId = configuration["FritzBot:QnA:KnowledgeBaseId"];
			_MissingAzureKey = String.IsNullOrEmpty(configuration["AzureServices:QnASubscriptionKey"]);
			_HttpClientFactory = httpClientFactory;
		}

		public async Task<DownloadPayload> Download()
		{

			// Exit now if we don't know how to connect to Azure
			if (_MissingAzureKey) return null;

			var client = _HttpClientFactory.CreateClient(MaintenanceClientName);
			var response = await client.GetAsync($"qnamaker/v4.0/knowledgebases/{_KnowledgeBaseId}/Prod/qna");
			response.EnsureSuccessStatusCode();

			var jsonBody = await response.Content.ReadAsStringAsync();
			return System.Text.Json.JsonSerializer.Deserialize<DownloadPayload>(jsonBody);

		}

		public async Task<QnAMakerResult> Query(string question) {

			// Exit now if we don't know how to connect to Azure
			if (_MissingAzureKey) return null;

			var payload = new StringContent($"{{\"question\": \"{question}\"}}", Encoding.UTF8, "application/json");

			var client = _HttpClientFactory.CreateClient(QuestionClientName);
			var response = await client.PostAsync($"knowledgebases/{_KnowledgeBaseId}/generateAnswer", payload);

			response.EnsureSuccessStatusCode();
			return JsonConvert.DeserializeObject<QnAMakerResult>(await response.Content.ReadAsStringAsync());

		}

		public async Task<Response> Update(UpdatePayload payload) {

			// Exit now if we don't know how to connect to Azure
			if (_MissingAzureKey) return null;

			var client = _HttpClientFactory.CreateClient(MaintenanceClientName);
			var content = new ByteArrayContent(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<UpdatePayload>(payload));
			var response = await client.PatchAsync($"qnamaker/v4.0/knowledgebases/{_KnowledgeBaseId}", content);
			response.EnsureSuccessStatusCode();

			var jsonBody = await response.Content.ReadAsStringAsync();
			return System.Text.Json.JsonSerializer.Deserialize<Response>(jsonBody);

		}

		/// <summary>
		/// Publish the Knowledgebase on QnAMaker so that we can start interacting with the updated data
		/// </summary>
		/// <returns></returns>
		public async Task Publish() {

			// Exit now if we don't know how to connect to Azure
			if (_MissingAzureKey) return;

			var client = _HttpClientFactory.CreateClient(MaintenanceClientName);
			var response = await client.PostAsync($"qnamaker/v4.0/knowledgebases/{_KnowledgeBaseId}", new StringContent(""));
			response.EnsureSuccessStatusCode();

		}

	}


}
