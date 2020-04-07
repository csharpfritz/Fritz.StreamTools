using System;
using System.Collections.Generic;
using System.Linq;
using Fritz.Chatbot;
using Fritz.Chatbot.Commands;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Fritz.StreamTools.Interfaces;
using Fritz.StreamTools.Models;
using Fritz.StreamTools.Services;
using Fritz.StreamTools.TagHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace Fritz.StreamTools.StartupServices
{
  public static class ConfigureServices
	{
		private static Dictionary<Type, string[]> _ServicesRequiredConfiguration;
		private static IConfiguration _Configuration;

		public static void Execute(IServiceCollection services, IConfiguration configuration, Dictionary<Type, string[]> servicesRequiredConfiguration)
		{
			_Configuration = configuration;
			_ServicesRequiredConfiguration = servicesRequiredConfiguration;

			services.AddSingleton<RundownItemRepository>();
			services.AddSingleton<RundownRepository>();
			services.AddSingleton<IRundownService, RundownService>();
			services.Configure<FollowerGoalConfiguration>(configuration.GetSection("FollowerGoal"));
			services.Configure<FollowerCountConfiguration>(configuration.GetSection("FollowerCount"));
			services.Configure<Dictionary<string, SoundFxDefinition>>(configuration.GetSection("FritzBot:SoundFxCommands"));
			services.Configure<GitHubConfiguration>(configuration.GetSection("GitHub"));
			services.AddSingleton<FollowerClient>();
			services.AddAspNetFeatures();
			services.AddStreamingServices(configuration);

			services.AddSingleton<IConfigureOptions<SignalrTagHelperOptions>, ConfigureSignalrTagHelperOptions>();
			services.AddSingleton<SignalrTagHelperOptions>(cfg => cfg.GetService<IOptions<SignalrTagHelperOptions>>().Value);

			services.AddSingleton<IAttentionClient, AttentionHub>();

			services.AddQnAFeatures();

			services.AddHostedService<FritzBot>();

			services.AddSingleton(new GitHubClient(new ProductHeaderValue("Fritz.StreamTools")));
	  	FritzBot.RegisterCommands(services);

			services.AddLazyCache();

			services.RegisterTwitchPubSub();

			RegisterConfiguredServices(services, configuration);
			RegisterGitHubServices(services, configuration);


		}

		private static void RegisterConfiguredServices(IServiceCollection services, IConfiguration configuration)
		{
			foreach (var configuredService in _ServicesRequiredConfiguration)
			{
				if (!configuredService.Value.Any(cs => configuration[cs] == null))
				{
					services.AddSingleton(typeof(IHostedService), configuredService.Key);
				}
			}
		}

		private static void RegisterGitHubServices(IServiceCollection services, IConfiguration configuration)
		{
			services.AddSingleton<GitHubRepository>();
			services.AddSingleton<GithubyMcGithubFaceClient>();

			services.AddTransient(_ => new GitHubClient(new ProductHeaderValue("Fritz.StreamTools"))
			{
				Credentials = new Credentials(_Configuration["GitHub:User"], _Configuration["GitHub:AuthenticationToken"])
			});

			services.AddHttpClient("GitHub", c =>
			{
				c.BaseAddress = new Uri("https://localhost:5001");
				c.DefaultRequestHeaders.Add("Accept", "applications/json");
			});

			services.AddHttpClient("DiscoverDotNet");
			services.AddHttpClient("ImageDescriptor");

			services.AddHttpClient("ShoutoutCommand", c =>
			{
				c.DefaultRequestHeaders.Add("client-id", configuration["StreamServices:Twitch:ClientId"]);
			});

			services.AddHostedService<GitHubService>();
		}

		private static void AddStreamingServices(this IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<Twitch.ConfigurationSettings>(configuration.GetSection("StreamServices:Twitch"));

			var provider = services.BuildServiceProvider();

			services.AddStreamService<TwitchService>(configuration,
				(c, l) => new TwitchService(c, l, provider.GetService<Fritz.Twitch.Proxy>(), provider.GetService<Fritz.Twitch.ChatClient>()),
				c => string.IsNullOrEmpty(c["StreamServices:Twitch:ClientId"]));		// Test to disable
			services.AddStreamService(configuration,
				(c, l) => new MixerService(c, l),                                   // Factory
				c => string.IsNullOrEmpty(c["StreamServices:Mixer:Channel"]));			// Test to disable
			services.AddStreamService(configuration,
				(c, l) => new FakeService(c, l),                                                          // Factory
				c => !bool.TryParse(c["StreamServices:Fake:Enabled"], out var enabled) || !enabled);			// Test to disable

			services.AddSingleton<StreamService>();

		}

		/// <summary>
		/// Generically configure stream service providers and register them with the DI container
		/// </summary>
		/// <typeparam name="TStreamService">Type of StreamService to create</typeparam>
		/// <param name="services">The DI services configuration object</param>
		/// <param name="configuration">Application Configuration to use to populate our service</param>
		/// <param name="factory">Callback method that defines how to instantiate the service</param>
		/// <param name="isDisabled">Callback test to determine whether to disable the service</param>
		private static void AddStreamService<TStreamService>(this IServiceCollection services,
			IConfiguration configuration,
			Func<IConfiguration, ILoggerFactory, TStreamService> factory,
			Func<IConfiguration, bool> isDisabled)
			where TStreamService : class, IStreamService, IHostedService
		{

			// Don't configure this service if it is disabled
			if (isDisabled(configuration))
			{
				return;
			}

			// Configure and grab a logger so that we can log information
			// about the creation of the services
			var provider = services.BuildServiceProvider();   // Build a 'temporary' instance of the DI container
			var loggerFactory = provider.GetService<ILoggerFactory>();

			var service = factory(configuration, loggerFactory);

			services.AddSingleton(service as IHostedService);
			services.AddSingleton(service as IStreamService);
			services.AddSingleton(service);

			if (service is IChatService chatService)
			{
				services.AddSingleton(chatService);
			}

		}

		/// <summary>
		/// Configure the standard ASP.NET Core services
		/// </summary>
		/// <param name="services"></param>
		private static void AddAspNetFeatures(this IServiceCollection services)
		{

			services.AddSignalR(options =>
			{

				options.KeepAliveInterval = TimeSpan.FromSeconds(5);

			}).AddMessagePackProtocol();

			services.AddRazorPages();

			services.AddMvc()
				.SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

		}

		private static void RegisterTwitchPubSub(this IServiceCollection services) {

			services.AddSingleton<Twitch.PubSub.Proxy>();

			services.AddHostedService<TwitchPubSubService>();

			//var provider = services.BuildServiceProvider();

			//var pubSub = new TwitchPubSubService(
			//provider,
			//provider.GetRequiredService<Twitch.PubSub.Proxy>(),
			//provider.GetRequiredService<IOptions<Twitch.ConfigurationSettings>>());
			//services.AddSingleton(pubSub as IHostedService);
			//services.AddSingleton(pubSub);

		}

		private static void AddQnAFeatures(this IServiceCollection services) {

			services.AddDbContext<Fritz.Chatbot.QnA.Data.QnADbContext>(options =>
			{
				options.UseNpgsql(_Configuration["FritzBot:QnA:ConnectionString"]);
			});

			services.AddTransient<Chatbot.QnA.QnAMaker.Proxy>();
			services.AddHttpClient<Fritz.Chatbot.QnA.QnAMaker.Proxy>(config =>
			{

				config.BaseAddress = new Uri(_Configuration["FritzBot:QnA:EndPoint"]);
				config.DefaultRequestHeaders.Add("Authorization", $"Ocp-Apim-Subscription-Key {_Configuration["AzureServices:QnASubscriptionKey"]}");
				config.DefaultRequestHeaders.Add("Accept", "application/json");
				config.DefaultRequestHeaders.Add("Content-Type", "application/json");

			});

		}

		private static bool IsTwitchEnabled {
			get { return string.IsNullOrEmpty(_Configuration["StreamServices:Twitch:ClientId"]); }
		}

	}
}
