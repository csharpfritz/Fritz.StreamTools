using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Services
{

	public class StreamService : IStreamService
	{

		private IEnumerable<IStreamService> _services;

		public StreamService(
			IEnumerable<IStreamService> services
		)
		{

			_services = services;

		}

		public int CurrentFollowerCount { get { return _services.Sum(s => s.CurrentFollowerCount); } }

		public int CurrentViewerCount { get { return _services.Sum(s => s.CurrentViewerCount); } }

		public string Name { get { return "Aggregate"; } }

		public IEnumerable<(string service, int count)> ViewerCountByService
		{
			get
			{

				return _services.Select(s => (s.Name, s.CurrentViewerCount));

			}
		}

		public IEnumerable<(string service, int count)> FollowerCountByService
		{
			get
			{

				return _services.Select(s => (s.Name, s.CurrentFollowerCount));

			}
		}


		public event EventHandler<ServiceUpdatedEventArgs> Updated
		{
			add
			{
				foreach (var s in _services)
				{
					s.Updated += value;
				}
			}
			remove
			{
				foreach (var s in _services)
				{
					s.Updated -= value;
				}
			}
		}

	}
}