using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class ShoutoutCommand : IBasicCommand2
	{
		private readonly HttpClient _HttpClient;

		public ShoutoutCommand(IHttpClientFactory httpClientFactory)
		{

			_HttpClient = httpClientFactory.CreateClient("ShoutoutCommand");

		}


		public string Trigger => "so";

		public string Description => "Issue a shout out to another streamer, promoting them on stream";

		public TimeSpan? Cooldown => TimeSpan.FromSeconds(5);

		public async Task Execute(IChatService chatService, string userName, bool isModerator, bool isBroadcaster, ReadOnlyMemory<char> rhs)
		{

			if (!(isModerator || isBroadcaster)) return;

			var rhsTest = rhs.ToString();
			if (rhsTest.StartsWith("@")) rhsTest = rhsTest.Substring(1);
			if (rhsTest.StartsWith("http")) return;
			if (rhsTest.Contains(" ")) return;

			rhsTest = WebUtility.UrlEncode(rhsTest);
			var result = await _HttpClient.GetAsync(rhsTest);
			if (result.StatusCode != HttpStatusCode.OK) return;

			await chatService.SendMessageAsync($"Please follow @{rhsTest} at: https://twitch.tv/{rhsTest}");

		}

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			throw new NotImplementedException();
		}
	}
}
