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
		public IEnumerable<RundownItem> Get()
		{
			return Repository.Get();
		}

		// GET: api/RundownItem/5
		[HttpGet("{id}", Name = "Get")]
		public RundownItem Get(int id)
		{
			return Repository.Get().First(i => i.ID == id);
		}

		// POST: api/RundownItem
		[HttpPost]
		public void Post([FromBody]RundownItem value)
		{
		}

		// PUT: api/RundownItem/5
		[HttpPut("{id}")]
		public void Put(int id, [FromBody]RundownItem value)
		{
			Repository.Update(id, value);
		}

		// DELETE: api/ApiWithActions/5
		[HttpDelete("{id}")]
		public void Delete(int id)
		{
			Repository.Delete(id);
		}
	}
}
