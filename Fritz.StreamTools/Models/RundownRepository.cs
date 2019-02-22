using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Models
{
  public class RundownRepository
  {
		private static string _Title = "Rundown Title";

		public void UpdateTitle(string title)
		{
			_Title = title;
		}

		public string GetTitle()
		{
			return _Title;
		}

  }
}
