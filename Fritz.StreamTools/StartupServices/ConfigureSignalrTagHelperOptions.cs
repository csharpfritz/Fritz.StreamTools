using System;
using System.IO;
using System.Linq;
using Fritz.StreamTools.TagHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fritz.StreamTools.StartupServices
{

	public class ConfigureSignalrTagHelperOptions : IConfigureOptions<SignalrTagHelperOptions>
	{

		private readonly IHostingEnvironment Env;
		private readonly ILogger Logger;

		public ConfigureSignalrTagHelperOptions(IHostingEnvironment env, ILogger<ConfigureSignalrTagHelperOptions> logger)
		{

			Env = env;
			Logger = logger;

		}

		public void Configure(SignalrTagHelperOptions options)
		{

			var folder = new DirectoryInfo(Path.Combine(Env.WebRootPath, "lib", "signalr"));

			var fileInfo = folder.Exists
				? folder.GetFiles("signalr-client-*.min.js").OrderByDescending(f => f.Name).FirstOrDefault()
				: null;

			if (fileInfo == null)
			{
				var error = "Required signalr-client library not found.";
				Logger.LogCritical(error);
				throw new InvalidOperationException(error);
			}

			options.ClientLibarySource = $"/lib/signalr/{fileInfo.Name}";

		}

	}

}