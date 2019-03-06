using Fritz.StreamLib.Core;
using System;
using System.Threading.Tasks;

namespace ConsoleChatbot
{
  public class ConsoleChatService : IChatService
  {
	public string Name => "Console";

	public bool IsAuthenticated => true;

	public string BotUserName => "ConsoleBot";

	public event EventHandler<ChatMessageEventArgs> ChatMessage;
	public event EventHandler<ChatUserInfoEventArgs> UserJoined;
	public event EventHandler<ChatUserInfoEventArgs> UserLeft;

	public void ConsoleMessageReceived(string message)
	{

	  ChatMessage.Invoke(this, new ChatMessageEventArgs
	  {
		Message = message,
		UserName = "ConsoleUser"
	  });

	}

	public Task<bool> BanUserAsync(string userName)
	{
	  throw new NotImplementedException();
	}

	public Task<bool> SendMessageAsync(string message)
	{
	  Console.Out.WriteLine(message);
	  return Task.FromResult(true);
	}

	public Task<bool> SendWhisperAsync(string userName, string message)
	{
	  Console.Out.WriteLine($"<<{userName}>> {message}");
	  return Task.FromResult(true);
	}

	public Task<bool> TimeoutUserAsync(string userName, TimeSpan time)
	{
	  throw new NotImplementedException();
	}

	public Task<bool> UnbanUserAsync(string userName)
	{
	  throw new NotImplementedException();
	}
  }

}
