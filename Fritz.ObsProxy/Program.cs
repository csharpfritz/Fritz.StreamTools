using Fritz.ObsProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
						.ConfigureServices((hostContext, services) => {
							services.AddHostedService<Worker>();
							services.AddSingleton<ObsClient>();
							services.AddTransient<BotClient>();
						});

var runningHost = host.Build();

ObsClient.OnDisconnect = () => {
	runningHost.StopAsync();
};

runningHost.Run();
