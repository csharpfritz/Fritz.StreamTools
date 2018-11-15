using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.Chatbot.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Fritz.StreamTools.Pages
{
	public class CodeSuggestionsModel : PageModel
	{

		public readonly CodeSuggestionsManager CodeSuggestionsManager;

		public CodeSuggestionsModel()
		{

			this.CodeSuggestionsManager = CodeSuggestionsManager.Instance;

		}


		public void OnGet()
		{

		}
	}
}
