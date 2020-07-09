using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using OBS.WebSocket.NET;
using OBS.WebSocket.NET.Types;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.ObsProxy
{

	public class ObsClient : IDisposable
	{
		private bool _DisposedValue;
		private ObsWebSocket _OBS;

		private readonly ILogger _Logger;
		private readonly IConfiguration _Configuration;
		private readonly string _IpAddress;

		public ObsClient(ILoggerFactory loggerFactory, IConfiguration configuration)
		{
			_Logger = loggerFactory.CreateLogger("ObsClient");
			_Configuration = configuration;
			_IpAddress = string.IsNullOrEmpty(configuration["ObsIpAddress"]) ? "127.0.0.1:4444" : configuration["ObsIpAddress"];
		}

		/// <summary>
		/// Establish a connection to OBS
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public Task Connect()
		{

			_OBS = new ObsWebSocket();
			_OBS.Connect($"ws://{_IpAddress}", "");

			return Task.CompletedTask;

		}

		public string ImageFolderLocation => _Configuration["ImageFolder"];

		public string CameraSource => _Configuration["CameraSource"];


		public string TakeScreenshot()
		{

			SourceScreenshotResponse response = null;
			if (string.IsNullOrEmpty(ImageFolderLocation))
			{
				response = _OBS.Api.TakeSourceScreenshot(CameraSource, embedPictureFormat: "png");
				var cleanString = response.ImageData.Replace("data:image/png;base64,", "");
				var bytes = Convert.FromBase64String(cleanString);
				_Logger.LogWarning($"Took screenshot from scene '{CameraSource}'");
				return ProcessImage(CameraSource, bytes);
			}
			else
			{

				try
				{
					var imageFileName = ImageFolderLocation + "\\test.png";
					response = _OBS.Api.TakeSourceScreenshot(CameraSource, embedPictureFormat: "png");
					var cleanString = response.ImageData.Replace("data:image/png;base64,", "");
					var bytes = Convert.FromBase64String(cleanString);
					var outString = ProcessImage(CameraSource, bytes);
					_Logger.LogWarning($"${DateTime.Now} Took screenshot from scene '{CameraSource}'");
					return outString;

				}
				catch (Exception e)
				{
					_Logger.LogError(e, "Error while taking screenshot");
					return null;
				}

			}

			return response.ImageData;

		}

		private string ProcessImage(string cameraSource, byte[] imageBytes)
		{

			var outString = string.Empty;

			using (var img = Image.Load(imageBytes)) {

				// TODO: Crop appropriately for the camerasource
				var memStream = new MemoryStream();
				img.Clone(ctx => ctx.Crop(new Rectangle(450, 0, 900, 450))).SaveAsPng(memStream);
				memStream.Position = 0;

				outString = Convert.ToBase64String(memStream.ToArray());
				memStream.Dispose();

			}

			return outString;

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
