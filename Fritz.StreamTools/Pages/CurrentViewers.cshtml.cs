using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Fritz.StreamTools.Pages
{
	public class CurrentViewersModel : PageModel
	{

		public CurrentViewersModel(
			StreamService streamService
		)
		{
			this.StreamService = streamService;
		}

		public StreamService StreamService { get; }

		public void OnGet()
		{

		}

	}
}