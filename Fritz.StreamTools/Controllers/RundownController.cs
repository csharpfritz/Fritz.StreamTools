using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.StreamTools.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fritz.StreamTools.Controllers
{
  [Produces("application/json")]
  [Route("api/rundown")]
  public class RundownController : Controller
	{
		public RundownRepository Repository { get; private set; }
		
		public RundownController(RundownRepository repo)
		{
			this.Repository = repo;
		}

		[HttpGet("title")]
		public IActionResult Get()
		{
			return Ok(Repository.GetTitle());
		}

		// PUT: api/Rundown/Title/
		[HttpPut("title")]
		public IActionResult Put([FromBody]string value)
		{
			Repository.UpdateTitle(value);
			return Ok(value);
		}
	}
}
