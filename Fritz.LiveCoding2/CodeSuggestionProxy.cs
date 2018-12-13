using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.Shell;
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

		internal event EventHandler<CodeSuggestion> OnNewCode;

		public CodeSuggestionProxy()
		{

			try
			{
				_client = new HubConnectionBuilder().WithUrl("http://localhost.fiddler:62574/followerstream?groups=codesuggestions")
				.Build();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Exception while initializing HubConnectionBuilder: {ex}");
			}

			_client.On<CodeSuggestion>("OnNewCode", x => OnNewCode?.Invoke(null, x));
			_client.Closed += _client_Closed;

		}

		public async Tasks.Task StartAsync()
		{

			try
			{
				await _client.StartAsync();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error while connecting: {ex}");
			}
			Debug.WriteLine("Completed starting connection to SignalR");

		}

		private async System.Threading.Tasks.Task _client_Closed(Exception arg)
		{
			throw new NotImplementedException();
		}

		internal static void Initialize()
		{

			if (_Instance != null) return;

			lock (_InstanceLock)
			{

				if (_Instance == null)
				{

					_Instance = new CodeSuggestionProxy();

				}

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

					ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
					{
						await _client.DisposeAsync();
					});

				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
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
