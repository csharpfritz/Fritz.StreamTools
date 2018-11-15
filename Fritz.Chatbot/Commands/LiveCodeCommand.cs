using System;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class LiveCodeCommand : IBasicCommand
	{


		public string Trigger => "code";

		public string Description => "Send a code suggestion to the broadcaster";

		public TimeSpan? Cooldown => TimeSpan.FromSeconds(0.5);

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			// !code file.cs 12 public string newProperty {get;set;}

			// TODO: Validation / error checking
			// TODO: Adult-language checking
			// TODO: Programming Language validation  ?Roslyn?
			// TODO: Blacklisting users
			// TODO: Blacklisting files / whitelisting files


			// TODO: Visual Studio plugin to list these suggestions
			// TODO: Add comment indicating the Twitch user who suggested the code

			// SqlMisterMagoo Cheered 100 on November 15, 2018

			var segments = rhs.ToString().Split(' ');

			var suggestion = new CodeSuggestion
			{
				UserName = userName,
				FileName = segments[0],
				LineNumber = int.Parse(segments[1]),
				Body = rhs.ToString().Substring(segments[0].Length + segments[1].Length + 2).Trim()
			};

			CodeSuggestionsManager.Instance.AddSuggestion(suggestion);

			return Task.CompletedTask;


		}

	}


}
