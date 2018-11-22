using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Fritz.LiveCoding
{



	/// <summary>
	/// This is the datasource we are providing to the 'Errors List' window in Visual Studio.  We will manage
	/// our collection of Code Suggestions in this class
	/// </summary>
	class CodeSuggestionSnapshot : WpfTableEntriesSnapshotBase
	{

		private readonly int _versionNumber;

		// We're not using an immutable list here but we cannot modify the list in any way once we've published the snapshot.
		public readonly List<CodeSuggestion> Suggestions = new List<CodeSuggestion>();

		public CodeSuggestionSnapshot NextSnapshot;

		internal CodeSuggestionSnapshot(int versionNumber = 0)
		{
			_versionNumber = versionNumber;
		}

		public override int Count
		{
			get
			{
				return this.Suggestions.Count;
			}
		}

		public override int VersionNumber
		{
			get
			{
				return _versionNumber;
			}
		}

		public override int IndexOf(int currentIndex, ITableEntriesSnapshot newerSnapshot)
		{
			// This and TranslateTo() are used to map errors from one snapshot to a different one (that way the error list can do things like maintain the selection on an error
			// even when the snapshot containing the error is replaced by a new one).
			//
			// You only need to implement Identity() or TranslateTo() and, of the two, TranslateTo() is more efficient for the error list to use.

			// Map currentIndex to the corresponding index in newerSnapshot (and keep doing it until either
			// we run out of snapshots, we reach newerSnapshot, or the index can no longer be mapped forward).
			var currentSnapshot = this;
			do
			{

				Debug.Assert(currentIndex >= 0);
				Debug.Assert(currentIndex < currentSnapshot.Count);

				currentIndex = currentSnapshot.Suggestions[currentIndex].NextIndex;

				currentSnapshot = currentSnapshot.NextSnapshot;
			}
			while ((currentSnapshot != null) && (currentSnapshot != newerSnapshot) && (currentIndex >= 0));

			return currentIndex;
		}

		/// <summary>
		/// This method formats the entries in the table for a code suggestion
		/// </summary>
		/// <param name="index">The position of the code suggestion in our collection</param>
		/// <param name="columnName">The name of the column in the error list window</param>
		/// <param name="content">The content to present in the column for our entry</param>
		/// <returns>Success indicator</returns>
		public override bool TryGetValue(int index, string columnName, out object content)
		{
			if ((index >= 0) && (index < this.Suggestions.Count))
			{

				var thisSuggestion = this.Suggestions[index];

				if (columnName == StandardTableKeyNames.DocumentName)
				{
					// We return the full file path here. The UI handles displaying only the Path.GetFileName().
					content = thisSuggestion.FileName;
					return true;
				}
				else if (columnName == StandardTableKeyNames.ErrorCategory)
				{
					content = "Live Coding";
					return true;
				}
				else if (columnName == StandardTableKeyNames.ErrorSource)
				{
					content = thisSuggestion.ViewerName;
					return true;
				}
				else if (columnName == StandardTableKeyNames.Line)
				{
					// Line and column numbers are 0-based (the UI that displays the line/column number will add one to the value returned here).
					content = thisSuggestion.LineNumber;

					return true;
				}
				else if (columnName == StandardTableKeyNames.Column)
				{
					var position = thisSuggestion.Span.Start;
					var line = position.GetContainingLine();
					content = position.Position - line.Start.Position;

					return true;
				}
				else if (columnName == StandardTableKeyNames.Text)
				{
					content = thisSuggestion.SuggestedCode;

					return true;
				}
				//else if (columnName == StandardTableKeyNames2.TextInlines)
				//{
				//	var inlines = new List<Inline>();

				//	inlines.Add(new Run("Spelling: "));
				//	inlines.Add(new Run(this.Suggestions[index].Span.GetText())
				//	{
				//		FontWeight = FontWeights.ExtraBold
				//	});

				//	content = inlines;

				//	return true;
				//}
				else if (columnName == StandardTableKeyNames.ErrorSeverity)
				{
					content = __VSERRORCATEGORY.EC_MESSAGE;

					return true;
				}
				else if (columnName == StandardTableKeyNames.ErrorSource)
				{
					content = ErrorSource.Other;

					return true;
				}
				else if (columnName == StandardTableKeyNames.BuildTool)
				{
					content = "Code Suggestion";

					return true;
				}
				//else if (columnName == StandardTableKeyNames.ErrorCode)
				//{
				//	content = this.Suggestions[index].Span.GetText();

				//	return true;
				//}
				else if ((columnName == StandardTableKeyNames.ErrorCodeToolTip) || (columnName == StandardTableKeyNames.HelpLink))
				{
					content = string.Format(CultureInfo.InvariantCulture, "http://www.bing.com/search?q={0}", this.Suggestions[index].Span.GetText());

					return true;
				}

				// We should also be providing values for StandardTableKeyNames.Project & StandardTableKeyNames.ProjectName but that is
				// beyond the scope of this sample.
			}

			content = null;
			return false;
		}

		public override bool CanCreateDetailsContent(int index)
		{
			return true;	// Yes, we can create details content --> we will have a suggestion from a viewer
			//return this.Suggestions[index].AlternateSpellings.Count > 0;
		}

		public override bool TryCreateDetailsStringContent(int index, out string content)
		{
			content = this.Suggestions[index].SuggestedCode;

			return (content != null);
		}
	}

}
