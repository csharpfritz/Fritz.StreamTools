using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;

namespace Fritz.Chatbot.Commands
{
	public class HypeCommand : IBasicCommand
	{
	
		private readonly IConfiguration Configuration;

		// TurricanDE Cheered 500 bits on November 1, 2018
		// MrDemonWolf Cheered 100 bits on November 1, 2018
		// Pharewings Cheered 100 bits on November 1, 2018

		public HypeCommand(IConfiguration configuration)
		{
			this.Configuration = configuration;
		}


		public string Trigger => "hype";

		public string Description => "Let's hype up the channel with some cool emotes";

		public TimeSpan? Cooldown => TimeSpan.FromSeconds(5);

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			var hypeText = Configuration["FritzBot:HypeCommand:TemplateText"];
			var repeatCount = int.Parse(Configuration["FritzBot:HypeCommand:RepeatCount"]);


			var sb = new StringBuilder();
			for (var i=0; i<repeatCount; i++) {
				if (i!=0)
				{
					sb.Append(" ");
				}
				sb.Append(hypeText);
			}

			await chatService.SendMessageAsync(sb.ToString());

		}
	}
}
