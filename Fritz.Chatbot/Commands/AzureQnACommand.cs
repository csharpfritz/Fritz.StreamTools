using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Fritz.Chatbot.Helpers;
using Fritz.Chatbot.QnA.QnAMaker;
using Fritz.ChatBot.Helpers;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fritz.Chatbot.Commands
{

	public class AzureQnACommand : IExtendedCommand
	{
		public string Name => "AzureQnA";
		public string Description => "Answer questions using Azure Cognitive Services and Jeff's FAQ on the LiveStream wiki";
		public int Order => 1;
		public bool Final => true;
		public TimeSpan? Cooldown => null;

		public Proxy Proxy { get; }

		private readonly ILogger<AzureQnACommand> _logger;

		public AzureQnACommand(QnA.QnAMaker.Proxy proxy, ILogger<AzureQnACommand> logger)
		{
			Proxy = proxy;
			_logger = logger;
		}

		public bool CanExecute(string userName, string fullCommandText)
		{

			return fullCommandText.Length >= 10 && fullCommandText.EndsWith("?");

		}

		public async Task Execute(IChatService chatService, string userName, string fullCommandText)
		{

			_logger.LogInformation($"Handling question: \"{fullCommandText}\" from {userName}");

			await Query(chatService, userName, fullCommandText);
		}

		public async Task Query(IChatService chatService, string userName, string query)
		{
			QnAMakerResult response = null;
			try
			{
				response = await Proxy.Query(query).OrTimeout();
			}
			catch (TimeoutException)
			{
				_logger.LogWarning($"Azure Services did not respond in time to question '{query}'");
				chatService.SendMessageAsync($"Unable to answer the question '{query}' at this time").Forget();
				return;
			}
			catch (Exception ex)
			{
				_logger.LogError($">>> Error while communicating with QnA service: {ex.ToString()}");
				return;
			}

			try
			{

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
			catch (Exception ex) when (_logger.LogAndSwallow("asking knowledgebase", ex))
			{

			}
		}

	}
}
