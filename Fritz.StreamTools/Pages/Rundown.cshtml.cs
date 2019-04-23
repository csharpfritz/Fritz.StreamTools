using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.StreamTools.Interfaces;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Fritz.StreamTools.Pages
{
	public class RundownModel : PageModel
	{

		public RundownModel(IRundownService service)
		{
			this.Service = service;
		}

		public IRundownService Service { get; private set; }

		public void OnGet()
		{

		}
	}
}
