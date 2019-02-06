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

		// GET: api/items
		[HttpGet]
		public IActionResult Get()
		{
			return Ok(Repository.Get());
		}

		// GET: api/items/5
		[HttpGet("{id}", Name = "Get")]
		public IActionResult Get(int id)
		{
			var outValue = Repository.Get().FirstOrDefault(i => i.ID == id);
			if (outValue == null) return NotFound();
			return Ok(outValue);
		}

		// POST: api/items
		[HttpPost]
		public IActionResult Post()
		{
				var largestItemId = Repository.Get().Max(i => i.ID);
				var newItem = new RundownItem() { ID = largestItemId + 10, Text = "New Item" };
				Repository.Add(newItem);
				return Ok(newItem);
		}

		// PUT: api/items/5
		[HttpPut("{id}")]
		public IActionResult Put(int id, [FromBody]RundownItem value)
		{
			Repository.Update(id, value);
			return Ok(value);
		}

		// DELETE: api/items/5
		[HttpDelete("{id}")]
		public IActionResult Delete(int id)
		{
			Repository.Delete(id);
			return NoContent();
		}
	}
}
