using System;
using System.Threading.Tasks;
using Fritz.Chatbot.Commands;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Fritz.LiveCoding
{

	internal class CodeSuggestionProxy : IDisposable
	{
		private CodeSuggestionProvider _provider;
		private ITextBuffer _buffer;
		private ITextSnapshot _currentSnapshot;
		private HubConnection _connection;

		internal CodeSuggestionProxy(CodeSuggestionProvider provider, ITextView textView, ITextBuffer buffer)
		{
			_provider = provider;
			_buffer = buffer;
			_currentSnapshot = buffer.CurrentSnapshot;

			// Get the name of the underlying document buffer
			// JTF: Might not be needed
			//ITextDocument document;
			//if (provider.TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
			//{
			//	this.FilePath = document.FilePath;

			//	// TODO we should listen for the file changing its name (ITextDocument.FileActionOccurred)
			//}

			// We're assuming we're created on the UI thread so capture the dispatcher so we can do all of our updates on the UI thread.
			// JTF: Not needed until we're ready to tag fixes
			//_uiThreadDispatcher = Dispatcher.CurrentDispatcher;

			this.Factory = new CodeSuggestionsFactory(this, new CodeSuggestionSnapshot());

			StartListeningToSignalR();

		}

		// TODO:  This URL needs to come from configuration settings
		private string SourceUrl => $"http://localhost:62574/followerstream?groups=codesuggestions";

		private void StartListeningToSignalR()
		{

			_connection = new HubConnectionBuilder()
								.WithUrl(SourceUrl)
								.Build();

			_connection.On<Chatbot.Commands.CodeSuggestion>("onNewCode", OnNewCode);

			_connection.Closed += async (error) =>
			{
				await Task.Delay(new Random().Next(0, 5) * 1000);
				await _connection.StartAsync();
			};

			_connection.StartAsync();


		}

		private void OnNewCode(Chatbot.Commands.CodeSuggestion suggestion)
		{

			// SnapshotSpan SHOULD be the location of the code we're going to suggest injecting

			var newSuggestion = new CodeSuggestion(
				new SnapshotSpan(),
				suggestion.UserName,
				suggestion.LineNumber,
				suggestion.FileName,
				suggestion.Body
			);

			(this.Factory.GetCurrentSnapshot() as CodeSuggestionSnapshot).Suggestions.Add(newSuggestion);


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

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					
				}

				_connection.DisposeAsync().GetAwaiter().GetResult();

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		~CodeSuggestionProxy()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}

}
