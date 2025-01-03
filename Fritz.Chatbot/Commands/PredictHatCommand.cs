using Azure.AI.OpenAI;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Fritz.Chatbot.Commands
{
	public class PredictHatCommand : IBasicCommand
	{
		public string Trigger => "hat";
		public string Description => "Describe which hat Fritz is wearing";
		public TimeSpan? Cooldown => TimeSpan.FromSeconds(30);

		private DateTimeOffset? _LastRun = DateTimeOffset.MinValue;
		private TimeSpan _LastRunCooldown = TimeSpan.FromMinutes(5);
		private HatMetadata _LastHatDetected = null!;

		private ScreenshotTrainingService _ObsConnection;
		//private readonly HatDescriptionRepository _Repository;
		private readonly IHubContext<ObsHub> _HubContext;
		private readonly string _AzureOpenAiEndpoint;
		private readonly string _AzureOpenAiKey;
		private readonly string _AzureOpenAiModel;

		public PredictHatCommand(IConfiguration configuration, ScreenshotTrainingService service, IHubContext<ObsHub> hubContext)
		{
			_AzureOpenAiEndpoint = configuration["AzureOpenAiEndpoint"];
			_AzureOpenAiKey = configuration["AzureOpenAiKey"];
			_AzureOpenAiModel = configuration["AzureOpenAiModel"];
			_ObsConnection = service;
			_HubContext = hubContext;
		}

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			if (!userName.Equals("csharpfritz", StringComparison.InvariantCultureIgnoreCase)
				&& _LastRun.HasValue && DateTimeOffset.Now - _LastRun < _LastRunCooldown)
			{
				// send a message about the last hat detected
				await chatService.SendMessageAsync($"@{userName} from my last analysis {Math.Round(DateTimeOffset.Now.Subtract(_LastRun.Value).TotalMinutes)} minutes ago I can tell you Fritz's hat is:");
				await chatService.SendMessageAsync(_LastHatDetected.Description);
				return;
			}

			var obsImage = await _ObsConnection.GetScreenshotFromObs();

			////////////////////////////


			HatMetadata hatDetected;

			try
			{
				hatDetected = await PredictHat(obsImage);
			}
			catch
			{
				await chatService.SendMessageAsync("There was an error detecting this hat.  Please try again in 30 seconds");
				return;
			}

			if (string.IsNullOrEmpty(hatDetected.Name))
			{
				await chatService.SendMessageAsync("There was an error detecting a hat.  Please try again in 30 seconds");
				return;
			}

			// send a message about the hat detected
			await chatService.SendMessageAsync($"@{userName} I can tell you Fritz's hat is: {hatDetected.Name}");
			await chatService.SendMessageAsync(hatDetected.Description);
			await chatService.SendMessageAsync(hatDetected.Conclusion);
			_LastHatDetected = hatDetected;
			_LastRun = DateTimeOffset.Now;

		}

		private async Task<HatMetadata> PredictHat(Stream obsImage)
		{

			var client = new AzureOpenAIClient(
					new Uri(_AzureOpenAiEndpoint),
					new ApiKeyCredential(_AzureOpenAiKey))
				.AsChatClient(_AzureOpenAiModel);

			var systemPrompt =
					"""
			You are an AI assistant that can detect and describe baseball-style
			hats from an image.  The user will provide an image of someone wearing a hat
			and they might be also wearing a headset.  Ignore the headset and focus on the hat.

			This hat comes from a collection that includes hats that fall into one of these categories:
			Sports Teams, Colleges, Marvel Comics, Microsoft logos, Star Wars, 
			Video Games, and other popular culture references.

			Be as descriptive as possible about the hat focusing on its design, colors, 
			logo placement, text, category, organization that the hat references, and any notable features.

			Limit the description to 3 sentences.

			Suggest a name for the hat based on the color, organization, or logo.

			If the hat references a sports team or college, you should conclude with that school's slogan or cheer.

			If the hat references a Marvel comic character, you should conclude with a comment about the hat in the tone of the references character or organization
		""";

			var file = new byte[obsImage.Length];
			obsImage.ReadExactly(file, 0, (int)obsImage.Length);
			var messages = new List<ChatMessage>
		{
				new ChatMessage(ChatRole.System, systemPrompt),
				new ChatMessage(ChatRole.User, new AIContent[] {
						new ImageContent(file, "image/webp"),  // 
						new TextContent("Generate a description of the hat"),  //  Generate a description of the hat
				})
		};

			var sw = Stopwatch.StartNew();
			var response = await client.CompleteAsync<HatMetadata>(messages, options: new ChatOptions { Temperature = 0.1f });

			Console.WriteLine($"Elapsed: {sw.Elapsed}");
			Console.WriteLine($"Predicted hat: {response.Result}");
			return response.Result;

		}

	}

	internal record HatMetadata(
		string Color,
		string Name,
		bool HasLogo,
		string? LogoShape,
		string Text,
		string Description,
		string Category,
		string LogoDescription,
		string Organization,
		string Conclusion);

}
