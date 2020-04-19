using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fritz.Chatbot.Helpers;
using Fritz.Chatbot.QnA;
using Fritz.Chatbot.QnA.Data;
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
		private readonly QuestionCacheService _QuestionCacheService;
		private readonly QnADbContext _Context;
		private static readonly Regex _UserNameRegEx = new Regex(@"(@\w+)");

		public AzureQnACommand(QnA.QnAMaker.Proxy proxy, ILogger<AzureQnACommand> logger, QuestionCacheService questionCacheService, QnADbContext context)
		{
			Proxy = proxy;
			_logger = logger;
			_QuestionCacheService = questionCacheService;
			_Context = context;
		}

		public bool CanExecute(string userName, string fullCommandText)
		{

			var allowedQuestionTargets = new[] { "@csharpfritz", "@thefritzbot" };
			var firstTests = fullCommandText.Length >= 10 && fullCommandText.EndsWith("?");
			if (!firstTests) return false;

			var matches = _UserNameRegEx.Matches(fullCommandText);
			if (matches.Count == 0) return true;

			for (var i=0; i<matches.Count; i++)
			{

				if (allowedQuestionTargets.Any(s => matches[i].Value.Equals(s, StringComparison.InvariantCultureIgnoreCase))) {
					return true;
				}

			}

			return false;

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
					await LogInaccurateAnswer(query, thisAnswer.Answer, (decimal)thisAnswer.Score);
					_QuestionCacheService.Add(userName, query, thisAnswer.Id);
					await chatService.SendMessageAsync($"@{userName}, I know the answer to your question ({thisAnswer.Id}): {thisAnswer.Answer}");
				}
				else if (thisAnswer.Score > 30)
				{
					await LogInaccurateAnswer(query, thisAnswer.Answer, (decimal)thisAnswer.Score);
					_QuestionCacheService.Add(userName, query, thisAnswer.Id);
					await chatService.SendMessageAsync($"I'm not certain, @{userName}, but perhaps this will help ({thisAnswer.Id}):  " + thisAnswer.Answer + $@"({thisAnswer.Score.ToString("0.0")}% certainty)");

				}
				else
				{
					await LogInaccurateAnswer(query);
					_logger.LogInformation($"Unable to find suitable answer to {userName}'s question: {query}");
				}

			}
			catch (Exception ex) when (_logger.LogAndSwallow("asking knowledgebase", ex))
			{

			}
		}

		private async Task LogInaccurateAnswer(string questionText, string? answer = null, decimal? answerPct = null) {

			_Context.UnansweredQuestions.Add(new UnansweredQuestion
			{
				QuestionText = questionText,
				AnswerTextProvided = answer,
				AnswerPct = answerPct,
				AskedDateStamp = DateTime.UtcNow
			});

			await _Context.SaveChangesAsync();

		}

	}
}
