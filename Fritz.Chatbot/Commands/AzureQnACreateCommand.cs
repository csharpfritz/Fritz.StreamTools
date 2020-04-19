using Fritz.StreamLib.Core;
using Fritz.Chatbot.QnA.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fritz.Chatbot.QnA;

namespace Fritz.Chatbot.Commands
{
	public class AzureQnACreateCommand : IBasicCommand2
	{
		public string Trigger { get; } = "q";
		public string Description { get; } = "Moderators can add new questions and answers to the stream knowledgebase";
		public TimeSpan? Cooldown { get; } = TimeSpan.FromSeconds(30);
		private readonly QnADbContext _Context;
		private readonly QuestionCacheService _CacheService;

		public AzureQnACreateCommand(QnADbContext context, QuestionCacheService cacheService) {

			_Context = context;
			_CacheService = cacheService;
		}

		public Task Execute(IChatService chatService, string userName, bool isModerator, bool isVip, bool isBroadcaster, ReadOnlyMemory<char> rhs)
		{
			if (!(isModerator || isBroadcaster)) return Task.CompletedTask;

			_CacheService.AddQuestionForModerator(userName, $"!q {rhs}");

			return Task.CompletedTask;

		}

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			throw new NotImplementedException();
		}
	}
}
