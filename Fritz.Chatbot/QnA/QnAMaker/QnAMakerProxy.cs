using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fritz.Chatbot.QnA.QnAMaker
{

	public class Proxy
	{
		private readonly HttpClient _Client;

		private readonly string _KnowledgeBaseId;


		public Proxy(HttpClient client, IConfiguration configuration)
		{
			_Client = client;
			this._KnowledgeBaseId = configuration["FritzBot:QnA:KnowledgeBaseId"];
		}

		public async Task<DownloadPayload> Download()
		{

			var response = await _Client.GetAsync($"/qnamaker/v4.0/knowledgebases/{_KnowledgeBaseId}/Prod/qna");
			response.EnsureSuccessStatusCode();

			var jsonBody = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<DownloadPayload>(jsonBody);

		}

		public async Task<Response> Update(UpdatePayload payload) {

			var content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes<UpdatePayload>(payload));
			var response = await _Client.PatchAsync($"/qnamaker/v4.0/knowledgebases/{_KnowledgeBaseId}", content);
			response.EnsureSuccessStatusCode();

			var jsonBody = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<Response>(jsonBody);

		}

	}


}
