using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fritz.StreamLib.Core
{
	public interface IChatService
  {
		string Name { get; }
		bool IsAuthenticated { get; }

		string CurrentProject { get; set; }

		event EventHandler<ChatMessageEventArgs> ChatMessage;
		event EventHandler<ChatUserInfoEventArgs> UserJoined;
		event EventHandler<ChatUserInfoEventArgs> UserLeft;

		Task<bool> SendMessageAsync(string message);
		Task<bool> SendWhisperAsync(string userName, string message);
		Task<bool> TimeoutUserAsync(string userName, TimeSpan time);
		Task<bool> BanUserAsync(string userName);
		Task<bool> UnbanUserAsync(string userName);
	}
}
