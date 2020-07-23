using Fritz.StreamLib.Core;
using Fritz.StreamTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.Chatbot.Commands
{
	public class ToDoCommand : IBasicCommand2
	{
		public string Trigger => "todo";
		public string Description => "Manage the to-do list displayed.  !todo add <text> to add an item, !todo done <id> to mark an item completed, !todo remove <id> to remove an item";
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
				var newKey = !_ToDos.Any() ? 1 : _ToDos.Max(t => t.Key) + 1;
				_ToDos.Add(newKey, (false, rhs.ToString().Substring(4).Trim()));
				await _HubContext.Clients.All.SendAsync("todo_new", newKey, _ToDos[newKey].text);
			}
			else if (arrArgs[0] == "remove" && int.TryParse(arrArgs[1], out var removeid) && _ToDos.Any(t => t.Key == removeid))
			{
				_ToDos.Remove(removeid);
				await _HubContext.Clients.All.SendAsync("todo_remove", removeid);
			}
			else if (arrArgs[0] == "done" && int.TryParse(arrArgs[1], out var id) && _ToDos.Any(t => t.Key == id))
			{
				var todo = _ToDos[id];
				todo.completed = true;
				_ToDos[id] = todo;
				await _HubContext.Clients.All.SendAsync("todo_done", id);
			}
			else if (arrArgs[0] == "clear" && int.TryParse(arrArgs[1], out var clearId) && _ToDos.Any(t => t.Key == id))
			{
				var todo = _ToDos[clearId];
				todo.completed = false;
				_ToDos[clearId] = todo;
				await _HubContext.Clients.All.SendAsync("todo_clear", clearId);
			}
			else if (arrArgs[0] == "speed" && float.TryParse(arrArgs[1], out var speed))
			{
				await _HubContext.Clients.All.SendAsync("todo_speed", speed);
			}

		}

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			throw new NotImplementedException();
		}

		public static Dictionary<int, (bool completed, string text)> ToDos { get { return _ToDos; } }

	}
}
