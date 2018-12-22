using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Fritz.LiveCoding2.Exceptions
{

	public class ProjectFileNotFoundException : Exception
	{
	}

	public class MultipleFilesFoundException : Exception
	{
		private List<(IVsHierarchy vsObject, string path)> fileList;

		public MultipleFilesFoundException(List<(IVsHierarchy vsObject, string path)> fileList)
		{
			this.fileList = fileList;
		}
	}

	public class LineDoesNotExistException : Exception {

	}

}
