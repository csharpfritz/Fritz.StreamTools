
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace StreamAnalytics
{
	public static class Function1
	{
		[FunctionName("Function1")]
		public static IActionResult Run(
						[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage message,
						TraceWriter log,
						[CosmosDB("MyCosmosAnalyticsDatabase", "NewFollowers", ConnectionStringSetting ="SomeEnvironmentVariable")]out NewFollower newFollower
			)
		{
			log.Info("C# HTTP trigger function processed a request.");

			var task = message.Content.ReadAsAsync<NewFollower>();
			Task.WaitAll();
			newFollower = task.Result;


			//string requestBody = new StreamReader(req.Body).ReadToEnd();
			//dynamic data = JsonConvert.DeserializeObject(requestBody);
			//name = name ?? data?.name;

			return newFollower != null
					? (ActionResult)new OkObjectResult($"New Follower on {newFollower.StreamService}: {newFollower.Handle}")
					: new BadRequestObjectResult("Bad format...");
		}
	}


}
