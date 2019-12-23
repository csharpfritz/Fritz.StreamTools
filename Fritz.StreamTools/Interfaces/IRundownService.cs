using Fritz.StreamTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Interfaces
{
  public interface IRundownService
  {
		string GetRundownTitle();
		void UpdateTitle(string title);
		RundownItem AddNewRundownItem();
		void UpdateRundownItem(int id, RundownItem item);
		void DeleteRundownItem(int idToDelete);
		List<RundownItem> GetAllItems();
		RundownItem GetItem(int id);
  }
}
