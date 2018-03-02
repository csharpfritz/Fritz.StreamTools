using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Fritz.StreamTools.Helpers;

namespace Test.Services.Mixer
{
	public partial class RestClient
	{
		public class SimulatedHttpMessageHandler : HttpMessageHandler
		{
			public List<RequestContext> RequestHistory { get; } = new List<RequestContext>();
			List<RequestTrigger> Triggers = new List<RequestTrigger>();

			public void AddTrigger(HttpMethod method, string path, Func<RequestContext, HttpContent> callback)
			{
				var trigger = new RequestTrigger { Method = method, Path = path, Callback = callback };
				Triggers.Add(trigger);
			}

			public RequestContext FindRequest(string path, HttpMethod method) =>  RequestHistory.FirstOrDefault(x => x.Path == path && x.Method == method);
			public RequestContext FindRequest(string path) => RequestHistory.FirstOrDefault(x => x.Path == path && x.Method == HttpMethod.Get);

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				var ctx = new RequestContext {
					Method = request.Method,
					Path = request.RequestUri.AbsolutePath,
					Headers = request.Headers.ToDictionary(a => a.Key, a => a.Value.First()),
					Query = HttpUtility.ParseQueryString(request.RequestUri.Query).ToDictionary()
				};
				RequestHistory.Add(ctx);

				var response = new HttpResponseMessage(HttpStatusCode.NotFound);

				foreach(var t in Triggers)
				{
					if (request.Method == t.Method && request.RequestUri.AbsolutePath.Equals(t.Path, StringComparison.InvariantCultureIgnoreCase))
					{
						var content = t.Callback(ctx);
						if (content != null)
						{
							response.Content = content;
							response.StatusCode = HttpStatusCode.OK;
						}
					}
				}

				return Task.FromResult(response);
			}
		}

		public class RequestContext
		{
			public HttpMethod Method { get; set; }
			public string Path { get; set; }
			public IDictionary<string, string> Headers { get; set; }
			public IDictionary<string, string> Query { get; set; }
		}

		public class RequestTrigger
		{
			public HttpMethod Method { get; set; }
			public string Path { get; set; }
			public Func<RequestContext, HttpContent> Callback { get; set; }
		}

	}
}
