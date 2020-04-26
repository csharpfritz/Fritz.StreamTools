using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using OBS.WebSocket.NET;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.ObsProxy
{

	public class ObsClient : IDisposable
	{
		private bool _DisposedValue;
		private ObsWebSocket _OBS;

		private Dictionary<string, string> _SceneCameraSourceLookup = new Dictionary<string, string> {
			{"v2 Full Face", "Webcam-ChromaKey" },
			{"v2 Full Code", "NDI - NUC" }
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

			// NOTE: need to update my OBS Websocket plugin to verify that this works

			var currentScene = _OBS.Api.GetCurrentScene();
			var sceneName = currentScene.Name;

			if (!_SceneCameraSourceLookup.ContainsKey(sceneName)) {
				_Logger.LogError($"Unable to identify the camera source in scene '{sceneName}' ");
				throw new Exception($"Unable to identify the camera source in scene '{sceneName}' ");
			}

			var sourceName = _SceneCameraSourceLookup[sceneName];
			var response = _OBS.Api.TakeSourceScreenshot(sceneName);
			return response.ImageFile;

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
