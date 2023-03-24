using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.ObsProxy;


public class ObsClient : IDisposable {
	private bool _DisposedValue;
	private static OBSWebsocket _OBS;

	private readonly ILogger _Logger;
	private readonly IConfiguration _Configuration;
	private readonly string _IpAddress;
	private readonly string _Password;

	public bool IsReady { get; set; } = false;

	public ObsClient(ILoggerFactory loggerFactory, IConfiguration configuration) {
		_Logger = loggerFactory.CreateLogger("ObsClient");
		_Configuration = configuration;
		_IpAddress = string.IsNullOrEmpty(configuration["ObsIpAddress"]) ? "127.0.0.1:4455" : configuration["ObsIpAddress"];
		_Password = configuration["ObsPassword"];

		if (_OBS == null) {
			_OBS = new OBSWebsocket();
			_OBS.Connected += _OBS_Connected;
			_OBS.Disconnected += (s, e) => {
				OnDisconnect();
			};
		}

	}

	/// <summary>
	/// Establish a connection to OBS
	/// </summary>
	/// <param name="port"></param>
	/// <returns></returns>
	public void Connect() {

		Task.Run(() => _OBS.ConnectAsync($"ws://{_IpAddress}", _Password));

	}

	public static Action OnDisconnect { get; set; } = () => { };

	private void _OBS_Connected(object sender, EventArgs e) {

		IsReady = true;
		var versionInfo = _OBS.GetVersion();
		Console.WriteLine($"Plugin Version: {versionInfo.PluginVersion}");
	}

	public string ImageFolderLocation => _Configuration["ImageFolder"];

	public string CameraSource => _Configuration["CameraSource"];


	public string TakeScreenshot() {

		Console.WriteLine($"IsConnected: {_OBS.IsConnected}");

		try {
			var imageFileName = System.IO.Path.GetTempFileName();
			_OBS.SaveSourceScreenshot(CameraSource, "png", imageFileName);

			var outString = string.Empty;
			using (var tempFile = File.OpenRead(imageFileName)) {

				outString = ProcessImage(CameraSource, tempFile);

			}
			File.Delete(imageFileName);
			_Logger.LogWarning($"${DateTime.Now} Took screenshot from scene '{CameraSource}'");
			return outString;

		}
		catch (Exception e) {
			_Logger.LogError(e, "Error while taking screenshot");
			return null;
		}

	}

	private string ProcessImage(string cameraSource, Stream image) {

		var outString = string.Empty;

		using (var img = Image.Load(image)) {

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

	protected virtual void Dispose(bool disposing) {
		if (!_DisposedValue) {
			if (disposing) {
				// TODO: dispose managed state (managed objects)
			}

			_OBS.Disconnect();
			_OBS = null;
			_DisposedValue = true;
		}
	}

	~ObsClient() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: false);
	}

	public void Dispose() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion



}
