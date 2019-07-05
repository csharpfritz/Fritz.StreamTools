using System;
using System.Collections.Generic;
using System.Linq;
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

	private static readonly List<string> AndThens = new List<string>();

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

	  if (!InternalCooldownCheck()) return Task.CompletedTask;

	  var cmd = Effects[cmdText];

	  SoundCooldowns[cmdText] = (cmdText == "andthen" ? CalculateAndThenCooldownTime() : DateTime.Now);

	  var fileToPlay = cmdText == "andthen" ? IdentifyAndThenFilename() : cmd.File;

	  var soundTask = this.HubContext.Clients.All.PlaySoundEffect(fileToPlay);
	  var textTask = chatService.SendMessageAsync($"@{userName} - {cmd.Response}");

	  return Task.WhenAll(soundTask, textTask);

		bool InternalCooldownCheck()
	  {

			if (cmdText == "andthen")
			{
				if (!CheckAndThenCooldown())
				{
					chatService.SendMessageAsync($"@{userName} - No AND THEN!");
					return false;
				}

				return true;
			}

			if (!SoundCooldowns.ContainsKey(cmdText)) return true;
			var cooldown = TimeSpan.FromSeconds(Effects[cmdText].Cooldown);
			return (SoundCooldowns[cmdText].Add(cooldown) < DateTime.Now);

	  }

	}

	private DateTime CalculateAndThenCooldownTime()
	{

	  if (!SoundCooldowns.ContainsKey("andthen")) return DateTime.Now;

	  if (AndThens.Count < 6) return SoundCooldowns["andthen"];

	  return DateTime.Now;

	}

	private bool CheckAndThenCooldown()
	{

	  var cooldown = TimeSpan.FromSeconds( Effects["andthen"].Cooldown);

	  if (SoundCooldowns.ContainsKey("andthen"))
	  {
			if (SoundCooldowns["andthen"].Add(cooldown) < DateTime.Now)
			{
				SoundCooldowns["andthen"] = DateTime.Now;
				AndThens.Clear();
				return true;
			} else
			{
				return (AndThens.Count != 6);
			}
	  }
	  return true;
	}

	private static readonly string[] AndThenFiles = new string[] {
	  "andthen1.mp3",
	  "andthen2.mp3",
	  "andthen3.mp3",
	  "andthen4.mp3",
	  "andthen5.mp3",
	  "andthen6.mp3" };

	private string IdentifyAndThenFilename()
	{

	  var available = new List<string>();
	  AndThenFiles.ToList().ForEach(a => { if (!AndThens.Contains(a)) available.Add(a); });
	  var random = new Random().Next(0, available.Count-1);
	  var theFile = available.Skip(random).First();
	  AndThens.Add(theFile);
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
