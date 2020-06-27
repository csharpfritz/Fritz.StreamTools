using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.Chatbot.Commands
{
	public class ToDoCommand : IBasicCommand2
	{
		public string Trigger => "todo";
		public string Description => "Manage the to-do list displayed";
		public TimeSpan? Cooldown => TimeSpan.FromSeconds(5);

		private static Dictionary<int, (bool completed, string text)> _ToDos = new Dictionary<int, (bool, string)>();
		private readonly IHubContext<ObsHub> _HubContext;

		public ToDoCommand(IHubContext<ObsHub> hubContext)
		{
			_HubContext = hubContext;
		}

		public async Task Execute(IChatService chatService, string userName, bool isModerator, bool isVip, bool isBroadcaster, ReadOnlyMemory<char> rhs)
		{

			if (!(isBroadcaster || isModerator)) return;

			var arrArgs = rhs.ToString().Split(' ');
			if (arrArgs[0] == "add") {
				_ToDos.Add(_ToDos.Count + 1, (false, rhs.ToString().Substring(4).Trim()));
				await _HubContext.Clients.All.SendAsync("todo_new", _ToDos.Count, _ToDos[_ToDos.Count].text);
			} else if (arrArgs[0] == "done" && int.TryParse(arrArgs[1], out var id)) {
				var todo = _ToDos[id];
				todo.completed = true;
				_ToDos[id] = todo;
				await _HubContext.Clients.All.SendAsync("todo_done", id);
			}

		}

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			throw new NotImplementedException();
		}

		public static Dictionary<int, (bool completed, string text)> ToDos { get { return _ToDos; } }

	}
}
