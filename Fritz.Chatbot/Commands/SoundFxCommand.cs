using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Fritz.Chatbot.Commands
{
  public class SoundFxCommand : IExtendedCommand
  {

	public SoundFxCommand(IHubContext<AttentionHub, IAttentionHubClient> hubContext)
	{
	  this.HubContext = hubContext;
	}


	public IHubContext<AttentionHub, IAttentionHubClient> HubContext { get; }

	public string Name => "SoundFxCommand";
	public string Description => "Play a fun sound effect in the stream";
	public int Order => 1;
	public bool Final => true;
	public TimeSpan? Cooldown => TimeSpan.FromSeconds(0);

	private static readonly Dictionary<string, (string text, string fileName, TimeSpan cooldown)> Effects = new Dictionary<string, (string text, string fileName, TimeSpan cooldown)>
	{
	  { "ohmy", ("Oh my... something strange is happening", "ohmy.mp3", TimeSpan.FromSeconds(30) ) },
	  { "andthen", ("... and then ...", "andthen1.mp3", TimeSpan.FromSeconds(120) ) },
	  { "andthen2", ("... and then ...", "andthen3.mp3", TimeSpan.FromSeconds(120) )},
	  { "andthen3", ("... and then ...", "andthen4.mp3", TimeSpan.FromSeconds(120) )},
	  { "andthen4", ("... and then ...", "andthen5.mp3", TimeSpan.FromSeconds(120) )},
	  { "andthen5", ("... and then ...", "andthen6.mp3", TimeSpan.FromSeconds(120) )},
	  { "andthen6", ("... and then ...", "andthen7.mp3", TimeSpan.FromSeconds(120) )},
	  { "javascript", ("Horses LOVE JavaScript!", "javascript.mp3", TimeSpan.FromSeconds(30) ) },
	};

	private static readonly Dictionary<string, DateTime> SoundCooldowns = new Dictionary<string, DateTime>();

	public bool CanExecute(string userName, string fullCommandText)
	{
	  if (!fullCommandText.StartsWith("!")) return false;
	  var cmd = fullCommandText.Substring(1).ToLowerInvariant();
	  if (!Effects.ContainsKey(cmd)) return false;

	  if (!SoundCooldowns.ContainsKey(cmd)) return true;
	  var cooldown = Effects[cmd].cooldown;
	  return (SoundCooldowns[cmd].Add(cooldown) < DateTime.Now);

	}

	public Task Execute(IChatService chatService, string userName, string fullCommandText)
	{

	  var cmdText = fullCommandText.Substring(1).ToLowerInvariant();
	  var cmd = Effects[cmdText];

	  SoundCooldowns[cmdText] = DateTime.Now;

	  var soundTask = this.HubContext.Clients.All.PlaySoundEffect(cmd.fileName);
	  var textTask = chatService.SendMessageAsync($"@{userName} - {cmd.text}");

	  return Task.WhenAll(soundTask, textTask);

	}
  }
}
