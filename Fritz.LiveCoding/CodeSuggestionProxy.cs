using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Fritz.LiveCoding
{

	internal class CodeSuggestionProxy
	{
		private CodeSuggestionProvider _provider;
		private ITextBuffer _buffer;
		private ITextSnapshot _currentSnapshot;

		internal CodeSuggestionProxy(CodeSuggestionProvider provider, ITextView textView, ITextBuffer buffer)
		{
			_provider = provider;
			_buffer = buffer;
			_currentSnapshot = buffer.CurrentSnapshot;

			// Get the name of the underlying document buffer
			// JTF: Might not be needed
			ITextDocument document;
			if (provider.TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
			{
				this.FilePath = document.FilePath;

				// TODO we should listen for the file changing its name (ITextDocument.FileActionOccurred)
			}

			// We're assuming we're created on the UI thread so capture the dispatcher so we can do all of our updates on the UI thread.
			// JTF: Not needed until we're ready to tag fixes
			//_uiThreadDispatcher = Dispatcher.CurrentDispatcher;

			this.Factory = new CodeSuggestionsFactory(this, new CodeSuggestionSnapshot());
		}

		// mike_from_playrgg cheered 600 on November 18, 2018
		// GarethHubball cheered 100 on November 18, 2018

		// Create connection to the Bot Http Endpoint

		// Get the current list of suggestions

		// Listen for suggestions

		// Raise an event / call a method to publish that suggestion

		// Dismiss / complete a suggestion

		public ITableEntriesSnapshotFactory Factory { get; internal set; }
		public object Dispatcher { get; }
	}

}
