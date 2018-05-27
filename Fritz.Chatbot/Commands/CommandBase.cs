using System;
using System.Linq;
using System.Threading.Tasks;
using Fritz.Chatbot.Commands;
using Fritz.StreamLib.Core;

public abstract class CommandBase : ICommand
{
	const char COMMAND_PREFIX = '!';

	abstract public string Name { get; }

	abstract public string Description { get; }

	virtual public int Order => 0;

	virtual public bool CanExecute(string userName, string fullCommandText)
	{
		if (string.IsNullOrEmpty(Name))
			return false;
		if (fullCommandText[0] != COMMAND_PREFIX)
			return false;

		var cmd = fullCommandText.AsSpan(1); // Skip command trigger
		int n = cmd.IndexOf(' ');
		if (n != -1)
			cmd = cmd.Slice(0, n);
		return cmd.Equals(Name.AsSpan(), StringComparison.OrdinalIgnoreCase);
	}

	abstract public Task Execute(IChatService chatService, string userName, string fullCommandText);
}
