using System;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Services.Mixer
{
	public interface IMixerChat
	{
		/// <summary>
		/// Raised each time a chat message is received
		/// </summary>
		event EventHandler<ChatMessageEventArgs> OnChatMessage;

		/// <summary>
		/// Connect to the chat server, and join our channel
		/// </summary>
		/// <param name="userId">Our userId</param>
		/// <param name="channelId">Out channelId</param>
		/// <returns></returns>
		Task ConnectAndJoinAsync(int userId, int channelId);

		/// <summary>
		/// Send a whisper to a user
		/// </summary>
		Task<bool> SendWhisperAsync(string userName, string message);

		/// <summary>
		/// Ban a user
		/// </summary>
		Task<bool> BanUserAsync(string userName);

		/// <summary>
		/// Write a chat message
		/// </summary>
		Task<bool> SendMessageAsync(string message);

		/// <summary>
		/// Timeout a user for the given time
		/// </summary>
		Task<bool> TimeoutUserAsync(string userName, TimeSpan time);

	}
}
