using System;
using System.Collections.Generic;

namespace Fritz.Chatbot.Commands
{
	public class CodeSuggestionsManager {

		public static readonly CodeSuggestionsManager Instance = new CodeSuggestionsManager();

		private CodeSuggestionsManager() { }

		private readonly List<CodeSuggestion> Suggestions = new List<CodeSuggestion>();

		public CodeSuggestion[] CurrentSuggestions => Suggestions.ToArray();

		public Action<CodeSuggestion> SuggestionAdded;

		internal void AddSuggestion(CodeSuggestion suggestion) {

			this.Suggestions.Add(suggestion);
			SuggestionAdded?.Invoke(suggestion);

		}

		public void RemoveSuggestion(CodeSuggestion suggestion) {

			this.Suggestions.Remove(suggestion);

		}

		public class SuggestionAddedEventArgs : EventArgs {

			public CodeSuggestion Suggestion { get; set; }

		}

	}


}
