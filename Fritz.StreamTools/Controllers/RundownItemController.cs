using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fritz.StreamTools.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fritz.StreamTools.Controllers
{
	[Produces("application/json")]
	[Route("api/items")]
	public class RundownItemController : Controller
	{
		public RundownRepository Repository { get; private set; }

		public RundownItemController(RundownRepository repo)
		{
			this.Repository = repo;
		}

		// GET: api/RundownItem
		[HttpGet]
		public IActionResult Get()
		{
			return Ok(Repository.Get());
		}

		// GET: api/RundownItem/5
		[HttpGet("{id}", Name = "Get")]
		public IActionResult Get(int id)
		{
			var outValue = Repository.Get().FirstOrDefault(i => i.ID == id);
			if (outValue == null) return NotFound();
			return Ok(outValue);
		}

		// POST: api/RundownItem
		[HttpPost]
		public void Post([FromBody]RundownItem value)
		{
		}

		// PUT: api/RundownItem/5
		[HttpPut("{id}")]
		public IActionResult Put(int id, [FromBody]RundownItem value)
		{
			Repository.Update(id, value);
			return Ok(value);
		}

		// DELETE: api/ApiWithActions/5
		[HttpDelete("{id}")]
		public IActionResult Delete(int id)
		{
			Repository.Delete(id);
			return NoContent();
		}
	}
}