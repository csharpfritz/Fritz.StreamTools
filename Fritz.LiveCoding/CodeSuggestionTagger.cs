using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.LiveCoding
{

/**

	class CodeSuggestionTagger : ITagger<IErrorTag>, IDisposable
	{
		private readonly CodeSuggestionProxy _spellChecker;
		private CodeSuggestionSnapshot _codeSuggestions;

		internal CodeSuggestionTagger(CodeSuggestionProxy codeProxy)
		{
			_spellChecker = codeProxy;
			_codeSuggestions = codeProxy.LastSpellingErrors;

			codeProxy.AddTagger(this);
		}

		internal void UpdateErrors(ITextSnapshot currentSnapshot, SpellingErrorsSnapshot spellingErrors)
		{
			var oldSpellingErrors = _codeSuggestions;
			_codeSuggestions = spellingErrors;

			var h = this.TagsChanged;
			if (h != null)
			{
				// Raise a single tags changed event over the span that could have been affected by the change in the errors.
				int start = int.MaxValue;
				int end = int.MinValue;

				if ((oldSpellingErrors != null) && (oldSpellingErrors.Suggestions.Count > 0))
				{
					start = oldSpellingErrors.Suggestions[0].Span.Start.TranslateTo(currentSnapshot, PointTrackingMode.Negative);
					end = oldSpellingErrors.Suggestions[oldSpellingErrors.Suggestions.Count - 1].Span.End.TranslateTo(currentSnapshot, PointTrackingMode.Positive);
				}

				if (spellingErrors.Count > 0)
				{
					start = Math.Min(start, spellingErrors.Errors[0].Span.Start.Position);
					end = Math.Max(end, spellingErrors.Errors[spellingErrors.Errors.Count - 1].Span.End.Position);
				}

				if (start < end)
				{
					h(this, new SnapshotSpanEventArgs(new SnapshotSpan(currentSnapshot, Span.FromBounds(start, end))));
				}
			}
		}

		public void Dispose()
		{
			// Called when the tagger is no longer needed (generally when the ITextView is closed).
			_spellChecker.RemoveTagger(this);
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			if (_codeSuggestions != null)
			{
				foreach (var error in _codeSuggestions.Suggestions)
				{
					if (spans.IntersectsWith(error.Span))
					{
						yield return new TagSpan<IErrorTag>(error.Span, new ErrorTag(PredefinedErrorTypeNames.Warning));
					}
				}
			}
		}
	}
	**/


}
