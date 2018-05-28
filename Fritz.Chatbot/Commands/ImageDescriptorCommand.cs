using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fritz.Chatbot.Models;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Fritz.Chatbot.Commands
{
	public class ImageDescriptorCommand : IExtendedCommand
	{
		private readonly string _AzureUrl;
		private readonly string _AzureApiKey;
		private string ImageUrl;

		public ImageDescriptorCommand(IConfiguration config)
		{
			_AzureUrl = config["FritzBot:VisionApiBaseUrl"];
			_AzureApiKey = config["FritzBot:VisionApiKey"];
		}

		public string Name => "Image";

		public string Description => "Inspect images and report to the chat room what they contain using Vision API";

		public int Order => 10;

		public bool CanExecute(string userName, string fullCommandText)
		{
			var imageCheckPattern = @"http(s)?:?(\/\/[^""']*\.(?:png|jpg|jpeg|gif))";
			var r = new Regex(imageCheckPattern, RegexOptions.IgnoreCase);

			// Match the regular expression pattern against a text string.
			var imageCheck = r.Match(fullCommandText);
			if (imageCheck.Captures.Count == 0)
				return false;
			this.ImageUrl = imageCheck.Captures[0].Value;
			return (imageCheck.Captures.Count > 0);
		}

		/// param name="fullCommandText" (this is the URL of the image we already found)
		public async Task Execute(IChatService chatService, string userName, string fullCommandText)
		{

			// TODO: Pull from ASP.NET Core Dependency Injection
			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _AzureApiKey);

			var requestParameters = "visualFeatures=Categories,Description,Color,Adult&language=en";
			var uri = _AzureUrl + "?" + requestParameters;

			var body = "{\"url\":\"" + ImageUrl + "\"}";
			var content = new StringContent(body, Encoding.UTF8, "application/json");

			var apiResponse = await client.PostAsync(uri, content);
			apiResponse.EnsureSuccessStatusCode();
			var result = await apiResponse.Content.ReadAsStringAsync();
			apiResponse.Dispose();

			var visionDescription = JsonConvert.DeserializeObject<VisionDescription>(result);

			if (visionDescription.adult.isAdultContent)
			{
				await chatService.SendMessageAsync($"Hey {userName} - we don't like adult content here!");
				// TODO: Timeout / Ban user
				return;
			}

			if (visionDescription.adult.isRacyContent)
			{
				await chatService.SendMessageAsync($"Hey {userName} - that's too racy ({visionDescription.adult.racyScore,0:P2}) for our chat room!");
				// TODO: Timeout user
				return;
			}

			var description = $"{userName} Photo ({visionDescription.description.captions[0].confidence,0:P2}): {visionDescription.description.captions[0].text}";

			await chatService.SendMessageAsync(description);

		}
	}
}
