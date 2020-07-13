using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.Chatbot.Commands
{
	public class TeamCommand : IExtendedCommand
	{

		private static HashSet<string> _Teammates = new HashSet<string>();
		private static Dictionary<string, DateTime> _TeammateCooldown = new Dictionary<string, DateTime>();
		private string _TeamName;
		private HttpClient _HttpClient;
		private readonly IHubContext<AttentionHub> _Context;
		private ILogger _Logger;

		public string Name { get; } = "Team Detection";
		public string Description { get; } = "Alert when a teammate joins the stream";
		public int Order { get; } = 1;
		public bool Final { get; } = false;
		public TimeSpan? Cooldown { get; } = TimeSpan.FromSeconds(5);

		public TeamCommand(IConfiguration configuration, ILoggerFactory loggerFactory, IHubContext<AttentionHub> context, IHttpClientFactory httpClientFactory)
		{
			_TeamName = configuration["StreamServices:Twitch:Team"];
			_Context = context;
			_Logger = loggerFactory.CreateLogger(nameof(TeamCommand));

			if (!string.IsNullOrEmpty(TwitchTokenConfig.Tokens?.access_token))
			{
				_HttpClient = httpClientFactory.CreateClient("TeamLookup");
				_HttpClient.BaseAddress = new Uri($"https://api.twitch.tv/kraken/teams/{_TeamName}");
				_HttpClient.DefaultRequestHeaders.Add("Client-ID", configuration["StreamServices:Twitch:ClientId"]);
				_HttpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.twitchtv.v5+json");

			}
			else
			{
				_Logger.LogError("Unable to create HttpClient for Twitch with Bearer token");
			}

		}

		public bool CanExecute(string userName, string fullCommandText)
		{
			if (!_Teammates.Any())
			{
				GetTeammates().GetAwaiter().GetResult();
			}

			var u = userName.ToLowerInvariant();
			var isTeammate = _Teammates.Contains(u);
			var recentShoutout = _TeammateCooldown.ContainsKey(u) && (DateTime.UtcNow.Subtract(_TeammateCooldown[u]).TotalHours > 1);

			return isTeammate && !recentShoutout;

		}

		public async Task Execute(IChatService chatService, string userName, string fullCommandText)
		{
			_TeammateCooldown[userName.ToLowerInvariant()] = DateTime.UtcNow;
			await _Context.Clients.All.SendAsync("Teammate", userName);
		}

		private async Task GetTeammates()
		{

			var response = await _HttpClient.GetStringAsync("");
			var team = JsonConvert.DeserializeObject<TeamResponse>(response);
			foreach (var user in team.users)
			{
				_Teammates.Add(user.name);
			}

		}


		internal class TeamResponse
		{
			public int _id { get; set; }
			public object background { get; set; }
			public string banner { get; set; }
			public DateTime created_at { get; set; }
			public string display_name { get; set; }
			public string info { get; set; }
			public string logo { get; set; }
			public string name { get; set; }
			public DateTime updated_at { get; set; }
			public User[] users { get; set; }
		}


		internal class User
		{
			public int _id { get; set; }
			public string broadcaster_language { get; set; }
			public DateTime created_at { get; set; }
			public string display_name { get; set; }
			public int followers { get; set; }
			public string game { get; set; }
			public string language { get; set; }
			public string logo { get; set; }
			public bool mature { get; set; }
			public string name { get; set; }
			public bool partner { get; set; }
			public string profile_banner { get; set; }
			public object profile_banner_background_color { get; set; }
			public string status { get; set; }
			public DateTime updated_at { get; set; }
			public string url { get; set; }
			public object video_banner { get; set; }
			public int views { get; set; }
		}


	}


}
