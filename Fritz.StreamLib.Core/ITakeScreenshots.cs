using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.StreamLib.Core
{
	public interface ITakeScreenshots
	{

		/// <summary>
		/// Take a screenshot and return the image as a byte array
		/// </summary>
		/// <returns>The screenshot image requested</returns>
		Task TakeScreenshot();

	}
}
