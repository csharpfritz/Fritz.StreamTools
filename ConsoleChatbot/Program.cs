using Fritz.StreamLib.Core;
using Fritz.StreamTools.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace ConsoleChatbot
{
	class Program
	{

		static void Main(string[] args)
		{

			Console.WriteLine("Interactive console for testing FritzBot");
			Console.WriteLine("Enter ZZ to exit console");

			var result = "";
			var consoleChat = new ConsoleChatService();
			var theBot = CreateFritzBot(consoleChat);
			while (result != "ZZ") {

				result = Console.ReadLine();

			}

		}

		private static FritzBot CreateFritzBot(IChatService chatService)
		{

			var serviceProvider = new StubServiceProvider();
			serviceProvider.Add<IChatService>(chatService);

			return new FritzBot(null, serviceProvider, new NullLoggerFactory());

		}

	}

}
