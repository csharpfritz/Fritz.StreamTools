using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fritz.Chatbot.QnA
{
	public class QuestionCacheService
	{

		private MemoryCache _Cache = new MemoryCache(new MemoryCacheOptions()
		{

			ExpirationScanFrequency = TimeSpan.FromSeconds(5),

		});

		public void Add(string userName, string questionText, long answerId)
		{

			_Cache.Set(userName, (questionText, answerId), TimeSpan.FromSeconds(30));

		}

		public (string questionText,long answerId) GetQuestionForUser(string userName)
		{

			return ((string,long))_Cache.Get(userName);

		}

		public void AddQuestionForModerator(string userName, string questionText) {

			_Cache.Set(userName, (questionText, 0), TimeSpan.FromMinutes(5));

		}


	}
}
