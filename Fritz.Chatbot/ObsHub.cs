using Fritz.StreamLib.Core;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Hubs
{

	public class ObsHub : Hub<ITakeScreenshots> {

		public static int ConnectedCount = 0;
		public static List<string> _ConnectionIds = new List<string>();

		public override async Task OnConnectedAsync()
		{

			_ConnectionIds.Add(Context.ConnectionId);

			ConnectedCount++;

			await base.OnConnectedAsync();

		}

		public override async Task OnDisconnectedAsync(Exception exception)
		{
			ConnectedCount--;

			await base.OnDisconnectedAsync(exception);
		}

		public async Task PostScreenshot(string imageData) {

			var cleanString = imageData.Replace("data:image/png;base64,", "");

			ScreenshotSink.Instance.OnScreenshotReceived(Convert.FromBase64String(cleanString));

		}

	}

	public interface IServerTakeScreenshot
	{
		Task TakeScreenshot();
	}

	public class ScreenshotReceivedEventArgs : EventArgs {

		public Stream Screenshot { get; set; }

	}

	public class ScreenshotSink {

		public static readonly ScreenshotSink Instance = new ScreenshotSink();

		private ScreenshotSink() { }

		public event EventHandler<ScreenshotReceivedEventArgs> ScreenshotReceived;

		public void OnScreenshotReceived(byte[] imageData) {

			var args = new ScreenshotReceivedEventArgs() { Screenshot = new MemoryStream(imageData) };
			ScreenshotReceived?.Invoke(null, args);

		}

	}

}
