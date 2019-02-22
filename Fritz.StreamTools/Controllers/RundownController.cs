using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.StreamTools.Interfaces;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fritz.StreamTools.Controllers
{
  [Produces("application/json")]
  [Route("api/rundown")]
  public class RundownController : Controller
	{

		private IRundownService rundownService;

		public RundownController(IRundownService rundownService)
		{
			this.rundownService = rundownService;
		}

		[HttpGet("title")]
		public IActionResult Get()
		{
			return Ok(rundownService.GetRundownTitle());
		}

		// PUT: api/Rundown/Title/
		[HttpPut("title")]
		public IActionResult Put([FromBody]string value)
		{
			rundownService.UpdateTitle(value);
			return Ok(value);
		}
	}
}
