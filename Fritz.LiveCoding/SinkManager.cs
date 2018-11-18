using Microsoft.VisualStudio.Shell.TableManager;
using System;

namespace Fritz.LiveCoding
{
	internal class SinkManager : IDisposable
	{
		private CodeSuggestionProvider _codeSuggestionProvider;
		private ITableDataSink _sink;

		public SinkManager(CodeSuggestionProvider codeSuggestionProvider, ITableDataSink sink)
		{
			this._codeSuggestionProvider = codeSuggestionProvider;
			this._sink = sink;
		}

		internal void AddCodeSuggester(CodeSuggestionProxy codeSuggester)
		{
			_sink.AddFactory(codeSuggester.Factory);
		}

		internal void RemoveCodeSuggester(CodeSuggestionProxy codeSuggester)
		{
			_sink.RemoveFactory(codeSuggester.Factory);
		}

		internal void UpdateSink()
		{
			_sink.FactorySnapshotChanged(null);
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{

					_codeSuggestionProvider.RemoveSinkManager(this);

				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~SinkManager() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

		internal void AddCodeSuggester(CodeSuggestionProxy codeSuggester)
		{
			throw new NotImplementedException();
		}

		internal void RemoveCodeSuggester(CodeSuggestionProxy codeSuggester)
		{
			throw new NotImplementedException();
		}

		internal void UpdateSink()
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
