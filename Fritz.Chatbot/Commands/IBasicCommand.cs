using System;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	/// <summary>
	/// Simple keyword based command interface
	/// </summary>
	public interface IBasicCommand
	{
		/// <summary>
		/// The command keyword
		/// </summary>
		string Trigger { get; }

		/// <summary>
		/// Description of the command (used by !help)
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Execute the command.
		/// </summary>
		/// <param name="chatService">The chatservice to use</param>
		/// <param name="userName">User that invoked the command</param>
		/// <param name="rhs">The remaining text after the trigger keyword</param>
		Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs);
	}
}
