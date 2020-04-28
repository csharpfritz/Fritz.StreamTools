using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using OBS.WebSocket.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.ObsProxy
{

	public class ObsClient : IDisposable
	{
		private bool _DisposedValue;
		private ObsWebSocket _OBS;

		private List<string> _CameraSources = new List<string> {
			{"NDI - NUC" },
			{"Webcam-ChromaKey" },
		};
		private readonly ILogger _Logger;
		private readonly IConfiguration _Configuration;

		public ObsClient(ILoggerFactory loggerFactory, IConfiguration configuration )
		{
			_Logger = loggerFactory.CreateLogger("ObsClient");
			_Configuration = configuration;
		}

		/// <summary>
		/// Establish a connection to OBS
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public Task Connect(short port) {

			_OBS = new ObsWebSocket();
			_OBS.Connect($"ws://127.0.0.1:{port}", "");

			return Task.CompletedTask;

		}

		public string ImageFolderLocation => _Configuration["ImageFolder"];

		public string TakeScreenshot() {

			var sourceName = _CameraSources.First();
			var response = _OBS.Api.TakeSourceScreenshot(sourceName,embedPictureFormat: "png");

			return response.ImageData;

		}


		#region Dispose OBS Connection

		protected virtual void Dispose(bool disposing)
		{
			if (!_DisposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
				}

				_OBS.Disconnect();
				_OBS = null;
				_DisposedValue = true;
			}
		}

		~ObsClient()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion

	}

}
