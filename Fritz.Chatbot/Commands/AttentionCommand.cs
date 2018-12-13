using System;
using System.Text;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;
using Microsoft.Extensions.Configuration;


namespace Fritz.Chatbot.Commands
{
  public class AttentionCommand : IBasicCommand
  {
	private readonly IConfiguration Configuration;

	public AttentionCommand(IConfiguration configuration)
	{
	  this.Configuration = configuration;
	}

	public string Trigger => "Fritz";

	public string Description => "Play audio queue to divert attention to chat";

	public TimeSpan? Cooldown => TimeSpan.FromSeconds(5);

	public async Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
	{
	  var player = new NetCoreAudio.Player();
	  await player.Play("hey_listen.wav");

	  var attentionText = Configuration["FritzBot:AttentionCommand:TemplateText"];

	  var sb = new StringBuilder();
	  sb.AppendFormat(attentionText, userName);

	  await chatService.SendMessageAsync(attentionText);
	}
  }
}
