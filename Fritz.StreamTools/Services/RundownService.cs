using Fritz.StreamTools.Interfaces;
using Fritz.StreamTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Services
{
  public class RundownService : IRundownService
  {
		private RundownItemRepository itemRepository;
		private RundownRepository repository;

		public RundownService(RundownItemRepository itemRepository, RundownRepository repository)
		{
			this.repository = repository;
			this.itemRepository = itemRepository;
		}

		public RundownItem AddNewRundownItem()
		{
			var largestItemId = itemRepository.Get().Max(i => i.ID);
			var newItem = new RundownItem() { ID = largestItemId + 10, Text = "New Item" };
			itemRepository.Add(newItem);
			return newItem;
		}

		public void DeleteRundownItem(int idToDelete)
		{
			itemRepository.Delete(idToDelete);	
		}

		public List<RundownItem> GetAllItems()
		{
			return itemRepository.Get().ToList();
		}

		public RundownItem GetItem(int id)
		{
			return itemRepository.Get().Where(i => i.ID == id).FirstOrDefault();
		}

		public string GetRundownTitle()
		{
			return repository.GetTitle();
		}

		public void UpdateRundownItem(int id, RundownItem item)
		{
			itemRepository.Update(id, item);
		}

		public void UpdateTitle(string title)
		{
			repository.UpdateTitle(title);
		}
  }
}
