﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Fritz.Chatbot.Models;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fritz.Chatbot.Commands
{
	public class ImageDescriptorCommand : IExtendedCommand
	{
		public string Name => "Image";
		public string Description => "Inspect images and report to the chat room what they contain using Vision API";
		public int Order => 10;
		public bool Final => true;

		private readonly string _AzureUrl;
		private readonly string _AzureApiKey;
		private string ImageUrl;
		private (string owner, string) _InstagramInfo;
		private ImageProviders Provider = ImageProviders.None;
		private string v1;
		private string v2;

		public TimeSpan? Cooldown => null;

		private static readonly Regex _UrlCheck = new Regex(@"http(s)?:?(\/\/[^""']*\.(?:png|jpg|jpeg|gif))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		internal static readonly Regex _InstagramCheck = new Regex(@"(?<InstagramUrl>https?:\/\/(www.)?instagram.com\/p\/[^\/]+\/?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private readonly IHttpClientFactory _ClientFactory;

		public ImageDescriptorCommand(IConfiguration config, IHttpClientFactory clientFactory) : this(config["FritzBot:VisionApiBaseUrl"], config["FritzBot:VisionApiKey"])
		{
			_ClientFactory = clientFactory;
		}

		public ImageDescriptorCommand(string azureUrl, string azureKey)
		{
			_AzureUrl = azureUrl;
			_AzureApiKey = azureKey;
		}

		public bool CanExecute(string userName, string fullCommandText)
		{

			// TODO: Check the cooldown on the image descriptor

			// Match the regular expression pattern against a text string.
			var imageCheck = _UrlCheck.Match(fullCommandText);
			var instagramCheck = _InstagramCheck.Match(fullCommandText);
			if (imageCheck.Captures.Count == 0 && !instagramCheck.Success)
				return false;

			if (imageCheck.Captures.Count != 0) this.ImageUrl = imageCheck.Captures[0].Value;
			if (instagramCheck.Captures.Count != 0)
			{
				this.Provider = ImageProviders.Instagram;
				this.ImageUrl = HandleInstagram(instagramCheck).GetAwaiter().GetResult();
				return !string.IsNullOrEmpty(this.ImageUrl);
			}

			return (!ImageUrl.Contains('#') && imageCheck.Captures.Count > 0) && ValidImageType(ImageUrl).GetAwaiter().GetResult();

			async Task<bool> ValidImageType(string url)
			{

				HttpResponseMessage response;

				using (var client = _ClientFactory.CreateClient("ImageDescriptor"))
				{
					try
					{
						var request = new HttpRequestMessage
						{
							Method = HttpMethod.Head,
							RequestUri = new Uri(url)
						};
						response = await client.SendAsync(request, cancellationToken: CancellationToken.None);
						response.EnsureSuccessStatusCode();
					}
					catch
					{
						return false;
					}

					return (response.Content.Headers.ContentType.MediaType.ToLowerInvariant().StartsWith("image/"));

				}

			}

		}


		/// param name="fullCommandText" (this is the URL of the image we already found)
		public async Task Execute(IChatService chatService, string userName, string fullCommandText)
		{

			// Cheer 100 themichaeljolley 01/3/19
			// Cheer 300 electrichavoc 01/3/19
			// Cheer 300 devlead 01/3/19
			// Cheer 100 brandonsatrom 01/3/19
			// Cheer 642 cpayette 01/3/19
			// Cheer 500 robertables 01/3/19
			// Cheer 100 johanb 01/3/19
			// Cheer 1000 bobtabor 01/3/19 

			var result = string.Empty;

			// TODO: Pull from ASP.NET Core Dependency Injection
			using (var client = _ClientFactory.CreateClient("ImageDescriptor"))
			{
				client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _AzureApiKey);

				var requestParameters = "visualFeatures=Categories,Description,Color,Adult&language=en";
				var uri = _AzureUrl + "?" + requestParameters;

				var body = JsonConvert.SerializeObject(new { url = ImageUrl });
				var content = new StringContent(body, Encoding.UTF8, "application/json");

				var apiResponse = await client.PostAsync(uri, content);

				try
				{
					apiResponse.EnsureSuccessStatusCode();
				}
				catch (Exception)
				{
					await chatService.SendMessageAsync($"Unable to inspect the image from {userName}");
					return;
				}
				result = await apiResponse.Content.ReadAsStringAsync();
				apiResponse.Dispose();
			}

			var visionDescription = JsonConvert.DeserializeObject<VisionDescription>(result);

			if (visionDescription.adult.isAdultContent && visionDescription.adult.adultScore > 0.85F)
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

			if (visionDescription.description.captions.Length == 0 && visionDescription.categories.Length > 0)
			{
				await chatService.SendMessageAsync($"No caption for the image submitted by {userName}, but it is: '{string.Join(',', visionDescription.categories.Select(c => c.name))}'");
				return;
			}

			var description = string.Empty;
			if (Provider == ImageProviders.Instagram)
			{
				description = $"{userName} Instagram({_InstagramInfo.owner}) ({visionDescription.description.captions[0].confidence,0:P2}): {visionDescription.description.captions[0].text}";
			} else 
			{
				description = $"{userName} Photo ({visionDescription.description.captions[0].confidence,0:P2}): {visionDescription.description.captions[0].text}";
			}

			await chatService.SendMessageAsync(description);

		}


		private async Task<string> HandleInstagram(Match instagramCheck)
		{

			// Cheer 200 phrakberg 03/3/19 

			var url = instagramCheck.Groups["InstagramUrl"].Value + "?__a=1";
			var imageUrl = string.Empty;
			using (var client = _ClientFactory.CreateClient("ImageDescriptor"))
			{

				var json = await client.GetStringAsync(url);
				var theResponse = JObject.Parse(json);

				imageUrl = theResponse["graphql"]["shortcode_media"]["display_url"].Value<string>();
				_InstagramInfo = (theResponse["graphql"]["shortcode_media"]["owner"]["full_name"].Value<string>(),
					"");

			}

			return imageUrl;

		}

	}

	public enum ImageProviders
	{
		None = 0,
		Instagram
	}
}
