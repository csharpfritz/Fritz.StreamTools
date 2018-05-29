using System;
using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public interface IExtendedCommand
	{
		string Name { get; }
		string Description { get; }

		/// <summary>
		/// Order by wich CanExecute are called, the higher the later
		/// </summary>
		int Order { get; }

		/// <summary>
		/// Cooldown for this command, or null
		/// </summary>
		/// <returns></returns>
		TimeSpan? Cooldown { get; }

		bool CanExecute(string userName, string fullCommandText);
		Task Execute(IChatService chatService, string userName, string fullCommandText);
	}
}
