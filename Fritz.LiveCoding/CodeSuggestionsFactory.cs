using Microsoft.VisualStudio.Shell.TableManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.LiveCoding
{

	class CodeSuggestionsFactory : TableEntriesSnapshotFactoryBase
	{
		private readonly CodeSuggestionProxy _codeSuggester;

		public CodeSuggestionSnapshot CurrentSnapshot { get; private set; }

		public CodeSuggestionsFactory(CodeSuggestionProxy codeSuggester, CodeSuggestionSnapshot codeSuggestions)
		{
			_codeSuggester = codeSuggester;

			this.CurrentSnapshot = codeSuggestions;
		}

		internal void UpdateErrors(CodeSuggestionSnapshot codeSuggestions)
		{
			this.CurrentSnapshot.NextSnapshot = codeSuggestions;
			this.CurrentSnapshot = codeSuggestions;
		}

		#region ITableEntriesSnapshotFactory members
		public override int CurrentVersionNumber
		{
			get
			{
				return this.CurrentSnapshot.VersionNumber;
			}
		}

		public override void Dispose()
		{
		}

		public override ITableEntriesSnapshot GetCurrentSnapshot()
		{
			return this.CurrentSnapshot;
		}

		public override ITableEntriesSnapshot GetSnapshot(int versionNumber)
		{
			// In theory the snapshot could change in the middle of the return statement so snap the snapshot just to be safe.
			var snapshot = this.CurrentSnapshot;
			return (versionNumber == snapshot.VersionNumber) ? snapshot : null;
		}
		#endregion
	}
}
