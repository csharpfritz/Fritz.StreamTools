using LazyCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Models
{

	public class GitHubRepository
	{

		public GitHubRepository(IAppCache appCache)
		{
			this.AppCache = appCache;
		}

		public IAppCache AppCache { get; }



	}

}
