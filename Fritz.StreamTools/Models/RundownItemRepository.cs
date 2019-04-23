using System.Collections.Generic;
using System.Linq;

namespace Fritz.StreamTools.Models
{
	public class RundownItemRepository
	{

			private static readonly List<RundownItem> _Items = new List<RundownItem>()
		{
			new RundownItem {ID=10, Text="PRE-SHOW: Set stream titles"},
			new RundownItem {ID=20, Text="PRE-SHOW: Send 'Stream starting soon' tweet"},
			new RundownItem {ID=30, Text="PRE-SHOW: Set background to 'Starting shortly' and start music"},
			new RundownItem {ID=40, Text="Start Stream!"},
			new RundownItem {ID=50, Text="Introduction"},
			new RundownItem {ID=60, Text="Music to Code By"},
			new RundownItem {ID=70, Text="Today's Hat"},
			new RundownItem {ID=80, Text="Housekeeping"},
			new RundownItem {ID=90, Text="Follower Goals"},
			new RundownItem {ID=110, Text="Pull Requests"},
			new RundownItem {ID=120, Text="New Code!"},
			new RundownItem {ID=130, Text="Log issue with last update"},
			new RundownItem {ID=140, Text="Commit and push source code"},
			new RundownItem {ID=150, Text="Next stream..."}
		};		

			public IEnumerable<RundownItem> Get()
			{

				return _Items.OrderBy(i => i.ID);

			}

			public void Add(RundownItem item)
			{
				_Items.Add(item);
			}

			public void Update(int id, RundownItem item)
			{
				Delete(id);
				_Items.Add(item);
			}

			public void Delete(int id)
			{

				_Items.Remove(_Items.FirstOrDefault(i => i.ID == id));

			}
	}
}
