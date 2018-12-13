using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.Diagnostics;
using Tasks = System.Threading.Tasks;

namespace Fritz.LiveCoding2
{
	internal class CodeSuggestionProxy : IDisposable
	{

		private static readonly object _InstanceLock = new object();
		private static CodeSuggestionProxy _Instance;
		private HubConnection _client;

		JoinableTaskFactory JoinableTaskFactory { get; }
		JoinableTaskCollection JoinableTaskCollection { get; }

		public CodeSuggestionProxy()
		{

			//this.JoinableTaskCollection = ThreadHelper.JoinableTaskContext.CreateCollection();
			//this.JoinableTaskFactory = ThreadHelper.JoinableTaskContext.CreateFactory(this.JoinableTaskCollection);

			try
			{
				_client = new HubConnectionBuilder().WithUrl("http://localhost.fiddler:62574/followerstream?groups=codesuggestions")
				.Build();
			} catch (Exception ex) {
				Debug.WriteLine($"Exception while initializing HubConnectionBuilder: {ex}");
			}

			//_client.On<CodeSuggestion>("onNewCode", AddCodeToOutputWindow);
			_client.On("OnNewCode", new Type[] { typeof(object) }, AddCode);
			_client.Closed += _client_Closed;

		}

		public async Tasks.Task StartAsync() {

			await ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
			{

				await _client.StartAsync();
				Debug.WriteLine("Completed starting connection to SignalR");

			});

			//return Tasks.Task.CompletedTask;

		}

		private System.Threading.Tasks.Task _client_Closed(Exception arg)
		{
			throw new NotImplementedException();
		}

		private System.Threading.Tasks.Task AddCode(object[] arg1)
		{
			AddCodeToOutputWindow(arg1[0] as CodeSuggestion);
			return System.Threading.Tasks.Task.CompletedTask;
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
		private object Threadhelper;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{

					JoinableTaskFactory.RunAsync(async delegate
					{
						await _client.DisposeAsync();
					});

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
}
