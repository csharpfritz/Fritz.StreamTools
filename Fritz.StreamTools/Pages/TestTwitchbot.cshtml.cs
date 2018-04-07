using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Fritz.StreamTools.Pages
{
	public class TestTwitchbotModel : PageModel
	{

		public TwitchService Service { get; }

		public TestTwitchbotModel(Services.TwitchService service)
		{
			Service = service;
		}

		[BindProperty]
		public string Message { get; set; }

		[BindProperty]
		public string UserName { get; set; }

		public void OnGet()
		{

		}

		public void OnPost()
		{

			Service.MessageReceived(false, false, Message, UserName);

		}

	}
}
