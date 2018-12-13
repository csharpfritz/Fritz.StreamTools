using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
	class Program
	{
		private static HubConnection _client;

		static void Main(string[] args)
		{
			// The code provided will print ‘Hello World’ to the console.
			// Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.
			Console.WriteLine("Hello World!");

			_client = new HubConnectionBuilder().WithUrl("http://localhost.fiddler:62574/followerstream?groups=codesuggestions")
.Build();
			_client.On("OnNewCode", new Type[] { typeof(object)
}, AddCode);
			_client.StartAsync();
			

			Console.ReadKey();

			// Go to http://aka.ms/dotnet-get-started-console to continue learning how to build a console app! 
		}

		private static Task AddCode(object[] arg1)
		{
			Console.WriteLine("Adding Code");
			return Task.CompletedTask;
		}
	}
}
