using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fritz.Chatbot.Models;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Fritz.Chatbot.Commands
{
	public class ImageDescriptorCommand : ICommand
	{
		private readonly string _AzureUrl;
		private readonly string _AzureApiKey;

		public ImageDescriptorCommand() { }

		public ImageDescriptorCommand(IConfiguration config)
		{
			_AzureUrl = config["FritzBot:VisionApiBaseUrl"];
			_AzureApiKey = config["FritzBot:VisionApiKey"];
		}

		public ImageDescriptorCommand(string azureUrl, string azureApiKey)
		{

			// This is ok for now...  :-)
            // no it's not

			_AzureUrl = azureUrl;
			_AzureApiKey = azureApiKey;

		}

		public IChatService ChatService { get; set; }
        

		public string Name => "ImageDescriptor";

		public string Description => "Inspect images and report to the chat room what they contain using Vision API";

        /// param name="fullCommandText" (this is the URL of the image we already found)
		public async Task Execute(string userName, string fullCommandText)
		{

			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _AzureApiKey);

			var requestParameters = "visualFeatures=Categories,Description,Color,Adult&language=en";
			var uri = _AzureUrl + "?" + requestParameters;

			var body = "{\"url\":\"" + fullCommandText + "\"}";
			var content = new StringContent(body, Encoding.UTF8, "application/json");

			var apiResponse = await client.PostAsync(uri, content);
			var result = await apiResponse.Content.ReadAsStringAsync();
			apiResponse.EnsureSuccessStatusCode();
			var visionDescription = JsonConvert.DeserializeObject<VisionDescription>(result);

			if (visionDescription.adult.isAdultContent)
			{
				await ChatService.SendMessageAsync($"Hey {userName} - we don't like adult content here!");
				// TODO: Timeout / Ban user
				return;
			}

			if (visionDescription.adult.isRacyContent)
			{
				await ChatService.SendMessageAsync($"Hey {userName} - that's too racy ({visionDescription.adult.racyScore,0:P2}) for our chat room!");
				// TODO: Timeout user
				return;
			}


			var description = $"{userName} Photo ({visionDescription.description.captions[0].confidence,0:P2}): {visionDescription.description.captions[0].text}";

			await ChatService.SendMessageAsync(description);

		}
	}
}
