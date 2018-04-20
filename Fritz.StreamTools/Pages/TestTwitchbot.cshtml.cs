using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Fritz.StreamTools.Pages
{
	public class TestTwitchbotModel : PageModel
	{

		public TwitchService Service { get; }
		public ILogger Logger { get; }
		public Twitch.Proxy TwitchProxy { get; }

		public TestTwitchbotModel(Services.TwitchService service, ILoggerFactory loggerFactory, Twitch.Proxy proxy)
		{
			Service = service;
			this.Logger = loggerFactory.CreateLogger("FritzBot");
			this.TwitchProxy = proxy;
		}

		[BindProperty]
		public string Message { get; set; }


		[BindProperty]
		public string UserName { get; set; }

		public TimeSpan? Uptime { get; set; }

		public void OnGet()
		{

			var sw = Stopwatch.StartNew();
			Uptime = TwitchProxy.Uptime;
			this.Logger.LogInformation($"Get uptime took {sw.ElapsedMilliseconds}ms");

			sw.Restart();
			var api = new TwitchLib.TwitchAPI(clientId: "t7y5txan5q662t7zj7p3l4wlth8zhv");
			var v5Stream = new TwitchLib.Streams.V5(api);
			var myStream = v5Stream.GetStreamByUserAsync("96909659").GetAwaiter().GetResult();
			var createdAt = myStream.Stream?.CreatedAt;
			this.Logger.LogInformation($"Get uptime took {sw.ElapsedMilliseconds}ms");


		}

		public void OnPost()
		{

			Service.MessageReceived(false, false, Message, UserName);

		}

	}
}
