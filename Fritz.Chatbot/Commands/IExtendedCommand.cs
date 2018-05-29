using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public interface IExtendedCommand
	{
		string Name { get; }
		string Description { get; }
		int Order { get; }

		bool CanExecute(string userName, string fullCommandText);
		Task Execute(IChatService chatService, string userName, string fullCommandText);
	}
}
