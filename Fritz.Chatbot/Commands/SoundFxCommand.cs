using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Fritz.Chatbot.Commands
{
	public class SoundFxCommand : IExtendedCommand
	{

		public SoundFxCommand(IHubContext<AttentionHub, IAttentionHubClient> hubContext, IOptions<Dictionary<string, SoundFxDefinition>> soundEffects)
		{
			this.HubContext = hubContext;

			Effects = soundEffects.Value;
		}

		public IHubContext<AttentionHub, IAttentionHubClient> HubContext { get; }

		public string Name => "SoundFxCommand";
		public string Description => "Play a fun sound effect in the stream";
		public int Order => 1;
		public bool Final => true;
		public TimeSpan? Cooldown => TimeSpan.FromSeconds(0);

		internal static Dictionary<string, SoundFxDefinition> Effects = new Dictionary<string, SoundFxDefinition>();
		/*
	{
	  { "ohmy", ("Oh my... something strange is happening", "ohmy.mp3", TimeSpan.FromSeconds(30) ) },
	  { "andthen", ("... and then ...", "andthen#.mp3", TimeSpan.FromSeconds(120) ) },
	  { "javascript", ("Horses LOVE JavaScript!", "javascript.mp3", TimeSpan.FromSeconds(30) ) },
		{ "rimshot", ("Ba Dum Tish!", "rimshot.mp3", TimeSpan.FromSeconds(60)) }
	};
	*/

		private static readonly Dictionary<string, List<string>> MultipleFileTriggers = new Dictionary<string, List<string>>();

		private static readonly Dictionary<string, DateTime> SoundCooldowns = new Dictionary<string, DateTime>();

		public bool CanExecute(string userName, string fullCommandText)
		{

			if (!fullCommandText.StartsWith("!")) return false;
			var cmd = fullCommandText.Substring(1).ToLowerInvariant();
			return Effects.ContainsKey(cmd);

		}

		public Task Execute(IChatService chatService, string userName, string fullCommandText)
		{

			var cmdText = fullCommandText.Substring(1).ToLowerInvariant();
			var cmd = Effects[cmdText];

			if (!InternalCooldownCheck()) return Task.CompletedTask;

			SoundCooldowns[cmdText] = (cmd.Files != null ? CalculateMultipleFileCooldownTime(cmd, cmdText) : DateTime.Now);


			var fileToPlay = cmd.Files != null ? IdentifyMultipleEffectsFilename(cmd, cmdText) : cmd.File;

			var soundTask = this.HubContext.Clients.All.PlaySoundEffect(fileToPlay);
			var textTask = chatService.SendMessageAsync($"@{userName} - {cmd.Response}");

			return Task.WhenAll(soundTask, textTask);

			bool InternalCooldownCheck()
			{

				if (cmdText == "andthen")
				{
					if (!CheckMultipleFilesCooldown(cmd, cmdText))
					{
						chatService.SendMessageAsync($"@{userName} - No AND THEN!");
						return false;
					}

					return true;
				}
				else if (cmd.Files != null)
				{
					if (!CheckMultipleFilesCooldown(cmd, cmdText))
					{
						// TODO: Something witty to indicate the message isn't available
						chatService.SendMessageAsync($"@{userName} - Scott is taking a break.. check back soon!");
						return false;
					}
					return true;
				}

				if (!SoundCooldowns.ContainsKey(cmdText)) return true;
				var cooldown = TimeSpan.FromSeconds(Effects[cmdText].Cooldown);
				return (SoundCooldowns[cmdText].Add(cooldown) < DateTime.Now);

			}

		}

		private DateTime CalculateMultipleFileCooldownTime(SoundFxDefinition cmd, string cmdTrigger)
		{

			if (!SoundCooldowns.ContainsKey(cmdTrigger))
			{
				MultipleFileTriggers.Add(cmdTrigger, new List<string>());
				return DateTime.Now;
			}

			if (MultipleFileTriggers[cmdTrigger].Count < cmd.Files.Length) return SoundCooldowns[cmdTrigger];

			return DateTime.Now;

		}

		private bool CheckMultipleFilesCooldown(SoundFxDefinition cmd, string cmdText)
		{

			var cooldown = TimeSpan.FromSeconds(Effects[cmdText].Cooldown);

			if (SoundCooldowns.ContainsKey(cmdText))
			{
				if (SoundCooldowns[cmdText].Add(cooldown) < DateTime.Now)
				{
					SoundCooldowns[cmdText] = DateTime.Now;
					MultipleFileTriggers[cmdText].Clear();
					return true;
				}
				else
				{
					return (MultipleFileTriggers[cmdText].Count != cmd.Files.Length);
				}
			}
			return true;
		}


		private string IdentifyMultipleEffectsFilename(SoundFxDefinition fxDefinition, string cmdText)
		{

			var available = new List<string>();
			fxDefinition.Files.ToList().ForEach(a => { if (!MultipleFileTriggers[cmdText].Contains(a)) available.Add(a); });
			var random = new Random().Next(0, available.Count - 1);
			var theFile = available.Skip(random).First();
			MultipleFileTriggers[cmdText].Add(theFile);
			return theFile;

		}
	}

	public class SoundFxConfig
	{
		public SoundFxDefinition[] SoundFx { get; set; }
	}

	public class SoundFxDefinition
	{

		public string Response { get; set; }

		public string File { get; set; }

		public string[] Files { get; set; }

		public int Cooldown { get; set; }

	}
}
