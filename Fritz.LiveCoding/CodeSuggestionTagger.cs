using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Fritz.LiveCoding
{
	public class CodeSuggestionTagger : ITagger<IErrorTag>, IDisposable
	{


		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			throw new NotImplementedException();
		}
	}

}
