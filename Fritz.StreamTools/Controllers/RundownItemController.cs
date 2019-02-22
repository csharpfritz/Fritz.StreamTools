using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fritz.StreamTools.Interfaces;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fritz.StreamTools.Controllers
{
	[Produces("application/json")]
	[Route("api/items")]
	public class RundownItemController : Controller
	{

		private IRundownService rundownService;

		public RundownItemController(IRundownService rundownService)
		{
			this.rundownService = rundownService;
		}

		// GET: api/items
		[HttpGet]
		public IActionResult Get()
		{
			return Ok(rundownService.GetAllItems());
		}

		// GET: api/items/5
		[HttpGet("{id}", Name = "Get")]
		public IActionResult Get(int id)
		{
			var outValue = rundownService.GetItem(id);
			if (outValue == null) return NotFound();
			return Ok(outValue);
		}

		// POST: api/items
		[HttpPost]
		public IActionResult Post()
		{
				var newItem = rundownService.AddNewRundownItem();
				return Ok(newItem);
		}

		// PUT: api/items/5
		[HttpPut("{id}")]
		public IActionResult Put(int id, [FromBody]RundownItem value)
		{
			rundownService.UpdateRundownItem(id, value);
			
			return Ok(value);
		}

		// DELETE: api/items/5
		[HttpDelete("{id}")]
		public IActionResult Delete(int id)
		{
			rundownService.DeleteRundownItem(id);			
			return NoContent();
		}
	}
}
