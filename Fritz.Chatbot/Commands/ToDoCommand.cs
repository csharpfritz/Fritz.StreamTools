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

		private readonly Dictionary<string, Func<string[], ToDoCommand, Task>> _Verbs = new Dictionary<string, Func<string[], ToDoCommand, Task>> {
			{"add", AddTodo },
			{"replace", ReplaceTodo },
			{"remove", RemoveTodo },
			{"done", DoneTodo },
			{"clear", ClearTodo },
			{"active", Activate },
			{"deactivate", Deactivate },
			{"speed", SetSpeed }
		};

		public ToDoCommand(IHubContext<ObsHub> hubContext)
		{
			_HubContext = hubContext;
		}

		public async Task Execute(IChatService chatService, string userName, bool isModerator, bool isVip, bool isBroadcaster, ReadOnlyMemory<char> rhs)
		{

			if (!(isBroadcaster || isModerator)) return;

			var arrArgs = rhs.ToString().Split(' ');
			if (_Verbs.ContainsKey(arrArgs[0]))
			{
				await _Verbs[arrArgs[0]](arrArgs, this);
			}

		}

		public Task Execute(IChatService chatService, string userName, ReadOnlyMemory<char> rhs)
		{
			throw new NotImplementedException();
		}

		private static async Task SetSpeed(string[] args, ToDoCommand cmd)
		{
			if (float.TryParse(args[1], out var speed))
			{
				await cmd._HubContext.Clients.All.SendAsync("todo_speed", speed);
			}
		}

		private static async Task Deactivate(string[] args, ToDoCommand cmd)
		{

			await cmd._HubContext.Clients.All.SendAsync("todo_deactivate");

		}

		private static async Task Activate(string[] args, ToDoCommand cmd)
		{

			if (int.TryParse(args[1], out var id) && _ToDos.Any(t => t.Key == id)) {

				await cmd._HubContext.Clients.All.SendAsync("todo_activate", id);

			}

		}

		private static async Task ClearTodo(string[] args, ToDoCommand cmd)
		{
			if (int.TryParse(args[1], out var clearId) && _ToDos.Any(t => t.Key == clearId))
			{
				var todo = _ToDos[clearId];
				todo.completed = false;
				_ToDos[clearId] = todo;
				await cmd._HubContext.Clients.All.SendAsync("todo_clear", clearId);
			}
		}

		private static async Task DoneTodo(string[] args, ToDoCommand cmd)
		{

			if (int.TryParse(args[1], out var id) && _ToDos.Any(t => t.Key == id))
			{
				var todo = _ToDos[id];
				todo.completed = true;
				_ToDos[id] = todo;
				await cmd._HubContext.Clients.All.SendAsync("todo_done", id);
			}

		}

		private static async Task ReplaceTodo(string[] args, ToDoCommand cmd)
		{
			if (int.TryParse(args[1], out var replaceId) && _ToDos.Any(t => t.Key == replaceId))
			{
				var thisTodo = _ToDos[replaceId];
				thisTodo.text = args[2];
				_ToDos[replaceId] = thisTodo;
				await cmd._HubContext.Clients.All.SendAsync("todo_replace", replaceId, args[2]);
			}

		}



		private static async Task RemoveTodo(string[] args, ToDoCommand cmd)
		{

			if (int.TryParse(args[1], out var removeid) && _ToDos.Any(t => t.Key == removeid))
			{
				_ToDos.Remove(removeid);
				await cmd._HubContext.Clients.All.SendAsync("todo_remove", removeid);
			}

		}

		private static async Task AddTodo(string[] args, ToDoCommand cmd)
		{
			var newKey = !_ToDos.Any() ? 1 : _ToDos.Max(t => t.Key) + 1;
			_ToDos.Add(newKey, (false, string.Join(' ', args).Substring(4).Trim()));
			await cmd._HubContext.Clients.All.SendAsync("todo_new", newKey, _ToDos[newKey].text);

		}


		public static Dictionary<int, (bool completed, string text)> ToDos { get { return _ToDos; } }

	}
}
