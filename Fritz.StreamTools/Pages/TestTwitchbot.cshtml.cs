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

			

		}

		public void OnPost()
		{

			Service.MessageReceived(false, false, Message, UserName);

		}

	}
}
