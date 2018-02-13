using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.ViewComponents
{

	public class FooterViewComponent : ViewComponent
	{

		public async Task<IViewComponentResult> InvokeAsync()
		{
			await Task.Delay(0);
			return View("default");

		}


	}
}
