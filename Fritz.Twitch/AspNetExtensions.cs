using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fritz.Twitch
{

	public static class AspNetExtensions
	{

		public static IServiceCollection AddTwitchClient(this IServiceCollection services)
		{

			services.AddHttpClient<Proxy>();
			services.AddSingleton<ChatClient>();

			return services;

		}

		private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// Convert a Unix timestamp to a .NET DateTime
		/// </summary>
		/// <param name="unixTime"></param>
		/// <returns></returns>
		public static DateTime ToDateTime(this long unixTime)
		{
				return epoch.AddSeconds(unixTime);
		}

	}

}
