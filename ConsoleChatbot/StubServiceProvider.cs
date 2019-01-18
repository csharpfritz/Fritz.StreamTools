using System;
using System.Collections.Generic;

namespace ConsoleChatbot
{
	public class StubServiceProvider : IServiceProvider
	{

		private readonly Dictionary<Type, object> _Services = new Dictionary<Type, object>();

		public object GetService(Type serviceType)
		{

			var result = _Services[serviceType];
			if (result is Type) {
				return Activator.CreateInstance(result as Type);
			}

			return result;

		}

		internal void Add<T>(T service)
		{

			_Services.Add(typeof(T), service);

		}
	}

}
