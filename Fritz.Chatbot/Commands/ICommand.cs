using Fritz.StreamLib.Core;
using System.Threading.Tasks;

namespace Fritz.Chatbot.Commands
{
	public interface ICommand
	{

		IChatService ChatService { get; set; }

		string Name { get; }

		Task Execute(params string[] args);

	}

}
