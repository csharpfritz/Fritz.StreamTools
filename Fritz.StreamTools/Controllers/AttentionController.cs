using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Fritz.StreamTools.Controllers
{
	public class AttentionController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Points() {
			return View();
		}

		public IActionResult TestClient()
		{
			return View();
		}
	}
}
