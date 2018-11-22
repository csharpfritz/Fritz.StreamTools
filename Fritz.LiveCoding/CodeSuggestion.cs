using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Text;

namespace Fritz.LiveCoding
{
	class CodeSuggestion
	{
		public readonly SnapshotSpan Span;

		public readonly string ViewerName = string.Empty;
		public readonly int LineNumber = 0;
		public readonly string FileName = string.Empty;
		public readonly string SuggestedCode = string.Empty;

		// This is used by SpellingErrorsSnapshot.TranslateTo() to map this error to the corresponding error in the next snapshot.
		public int NextIndex = -1;

		public CodeSuggestion(SnapshotSpan span, string viewerName, int lineNumber, string fileName, string suggestedCode)
		{
			this.Span = span;

			this.ViewerName = viewerName;
			this.LineNumber = lineNumber;
			this.FileName = fileName;
			this.SuggestedCode = suggestedCode;

		}

	}


}
