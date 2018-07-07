using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public class UptimeCommand : IBasicCommand
	{
		public string Trigger => "uptime";
		public string Description => "Report how long the stream has been on the air";
		public TimeSpan? Cooldown => TimeSpan.FromMinutes(1);

		public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{

			if (!(chatService is IStreamService svc))
			{
				return;
			}

			var uptime = await svc.Uptime();
			if (uptime.HasValue)
			{
				await chatService.SendMessageAsync($"The stream has been up for {uptime.Value.ToString(@"hh\:mm\:ss")}");
			}
			else
			{
				await chatService.SendMessageAsync("Stream is offline");
			}
		}
	}
}
