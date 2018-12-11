using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System;

namespace Fritz.LiveCoding2
{
	internal class CodeSuggestionProxy : IDisposable
	{

		private static readonly object _InstanceLock = new object();
		private static CodeSuggestionProxy _Instance;
		private HubConnection _client;

		private CodeSuggestionProxy()
		{

			_client = new HubConnectionBuilder().WithUrl("http://localhost:62574/followerstream?groups=codelist").Build();

			_client.On<CodeSuggestion>("onNewCode", AddCodeToOutputWindow);
			_client.StartAsync();

		}

		internal static void Initialize()
		{

			if (_Instance != null) return;

			lock (_InstanceLock) {

				if (_Instance == null) {

					_Instance = new CodeSuggestionProxy();

				}

			}

		}

		private void AddCodeToOutputWindow(CodeSuggestion suggestion) {

		Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

			IVsOutputWindowPane pane;
			var guidGeneralPane = VSConstants.GUID_OutWindowGeneralPane;
			CodeSuggestionsPackage.OutputWindow.GetPane(ref guidGeneralPane, out pane);
			if (pane != null)
			{
				pane.OutputString($"New code suggestion from {suggestion.UserName}");
			}

		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					 _client.DisposeAsync().GetAwaiter().GetResult();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~CodeSuggestionProxy() {
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
		#endregion

	}

	public class CodeSuggestion
	{

		public string UserName { get; set; }

		public string FileName { get; set; }

		public int LineNumber { get; set; }

		public string Body { get; set; }


	}
}
