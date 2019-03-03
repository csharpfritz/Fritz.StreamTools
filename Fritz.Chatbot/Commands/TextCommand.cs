using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{

	public class TextCommand : IExtendedCommand
	{

		// Cheer 143 cpayette 03/3/19 

		public string Name => "TextCommand";
		public string Description => "Return a simple line of text";
		public int Order => 0;
		public bool Final => true;
		public TimeSpan? Cooldown => TimeSpan.FromSeconds(5);

		private static readonly Dictionary<string, string> _Commands = new Dictionary<string, string>
		{
			{ "music", "Jeff plays 'Music to Code By' from Carl Franklin: http://mtcb.pwop.com" },
			{ "discord", "Join us on the Fritz and Friends Discord server at: https://discord.gg/RnJhrJq" },
			{ "github", "Checkout Jeff's GitHub at: https://github.com/csharpfritz" },
			{ "keyboard", "Jeff uses a Vortex Race 3 with Cherry MX Blue switches, details on his blog at: https://jeffreyfritz.com/2018/07/mechanical-keyboards-i-just-got-one-and-why-you-need-one-too/"  },
			{ "blog", "Jeff's blog is at: https://jeffreyfritz.com" },
			{ "lurk", "is stepping away from keyboard and lurking" }
		};

		public bool CanExecute(string userName, string fullCommandText)
		{

			if (!fullCommandText.StartsWith("!")) return false;
			var cmd = fullCommandText.Substring(1).ToLowerInvariant();
			return _Commands.ContainsKey(cmd);

		}

		public Task Execute(IChatService chatService, string userName, string fullCommandText)
		{

			var cmd = fullCommandText.Substring(1).ToLowerInvariant();

			if (cmd == "lurk")
				return chatService.SendMessageAsync($"@{userName} {_Commands[cmd]}");

			return chatService.SendMessageAsync($"@{userName} - {_Commands[cmd]}");

		}
	}

}
