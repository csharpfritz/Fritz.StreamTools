using System;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class LiveCodeCommand : IBasicCommand
	{


		public string Trigger => "code";

		public string Description => "Send a code suggestion to the broadcaster in the format: !code <filename> <linenumber> <code>";

		public TimeSpan? Cooldown => TimeSpan.FromSeconds(0.5);

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			// !code file.cs 12 public string newProperty {get;set;}

			// TODO: Validation / error checking
			// TODO: Adult-language checking
			// TODO: Programming Language validation  ?Roslyn?
			// TODO: Blacklisting users
			// TODO: Blacklisting files / whitelisting files
			// NOTE: opened an issue #166 to track these

			// TODO: Visual Studio plugin to list these suggestions
			// TODO: Add comment indicating the Twitch user who suggested the code
			// TODO: Twitch extension that shows editor at the current place I'm working in VS / VS Code

			// SqlMisterMagoo Cheered 100 on November 15, 2018
			// Pakmanjr Cheered 2000 on November 15, 2018
			// Quiltoni cheered 300 on December 9, 2018
			// RobertTables cheered 200 on December 9, 2018
			// stho32 cheered 100 on December 9, 2018
			// johanb cheered 100 on December 9, 2018
			// ziliel47 cheered 100 on December 9, 2018

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
