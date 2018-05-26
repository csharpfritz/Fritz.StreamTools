using Fritz.StreamLib.Core;
using System.Threading.Tasks;

namespace Fritz.Chatbot.Commands
{
	public interface ICommand
	{

		IChatService ChatService { get; set; }

		string Name { get; }

		string Description { get; }

		bool CanExecute(string userName, string fullCommandText);

		int Order { get;}


		// Could this be string userName, string command??

		Task Execute(string userName, string fullCommandText);

	}

}
