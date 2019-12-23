using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Xml;
using System.Diagnostics;

namespace Fritz.Chatbot.Commands
{

  public class TextCommand : IExtendedCommand
  {

	// Cheer 143 cpayette 03/3/19
	// Cheer 142 cpayette 2/06/19
	// Cheer 150 ramblinggeek 2/06/19

	public TextCommand(IConfiguration configuration)
	{
	  this.Configuration = configuration;

	  var children = Configuration.GetSection("FritzBot:TextCommand").GetChildren().ToArray();
	  foreach (var cmd in children)
	  {
			_Commands.Add(cmd.GetValue<string>("command"), cmd.GetValue<string>("response"));
	  }

	}

	public string Name => "TextCommand";
	public string Description => "Return a simple line of text";
	public int Order => 0;
	public bool Final => true;
	public TimeSpan? Cooldown => TimeSpan.FromSeconds(5);

	public IConfiguration Configuration { get; }

	internal static readonly Dictionary<string, string> _Commands = new Dictionary<string, string>();

	internal static bool IsCommand(string commandText)
	{

	  return _Commands.ContainsKey(commandText);

	}

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

			if (!_Commands[cmd].Contains('\n'))
			{
				return chatService.SendMessageAsync($"@{userName} - {_Commands[cmd]}");
			} else {
				var messages = _Commands[cmd].Split('\n');

				Task firstTask = null;
				Task previousTask = null;
				foreach (var msg in messages)
				{
					if (firstTask == null)
					{
						firstTask = chatService.SendMessageAsync(msg);
						previousTask = firstTask;
					}
					else
					{
						var thisTask = chatService.SendMessageAsync(msg);
						previousTask.ContinueWith((o) => thisTask);
						previousTask = thisTask;
					}
				}
				return firstTask;
			}

		}
  }


}
