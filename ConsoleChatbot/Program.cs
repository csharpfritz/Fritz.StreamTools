using Fritz.Chatbot.Commands;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

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
			theBot.StartAsync(new System.Threading.CancellationToken());
			while (result != "ZZ") {

				result = Console.ReadLine();
				consoleChat.ConsoleMessageReceived(result);

			}

		}

		private static FritzBot CreateFritzBot(IChatService chatService)
		{

			var serviceCollection = new ServiceCollection();
			serviceCollection.AddSingleton<IChatService>(chatService)
				.AddLogging();

			var config = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", true)
				.AddUserSecrets("78c713a0-80e0-4e16-956a-33cf16f08a02")		// Same as Fritz.StreamTools
				.Build();
			serviceCollection.AddSingleton<IConfiguration>(config);

			FritzBot.RegisterCommands(serviceCollection);
			var svcProvider = serviceCollection.BuildServiceProvider();
			var loggerFactory = svcProvider.GetService<ILoggerFactory>()
				.AddConsole(LogLevel.Information);

			return new FritzBot(config, svcProvider, loggerFactory);

		}

	}

}
