using Fritz.StreamLib.Core;
using System;
using System.Threading.Tasks;

namespace Test.Chatbot
{
  public class FakeChatService : IChatService
  {
	public string Name => "FakeService";

	public bool IsAuthenticated => true;

	public event EventHandler<ChatMessageEventArgs> ChatMessage;
	public event EventHandler<ChatUserInfoEventArgs> UserJoined;
	public event EventHandler<ChatUserInfoEventArgs> UserLeft;
	public string MessageSent { get; set; }
	public Task<bool> BanUserAsync(string userName)
	{
	  throw new NotImplementedException();
	}

	public Task<bool> SendMessageAsync(string message)
	{
	  MessageSent = message;
	  return Task.FromResult(true);
	}

	public Task<bool> SendWhisperAsync(string userName, string message)
	{
	  throw new NotImplementedException();
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
