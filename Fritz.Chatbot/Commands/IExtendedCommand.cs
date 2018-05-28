using System.Threading.Tasks;
using Fritz.StreamLib.Core;

namespace Fritz.Chatbot.Commands
{
	public interface IExtendedCommand
	{

		string Name { get; }

		string Description { get; }

		bool CanExecute(string userName, string fullCommandText);

		int Order { get; }

		// Could this be string userName, string command??

		Task Execute(IChatService chatService, string userName, string fullCommandText);

	}

}
