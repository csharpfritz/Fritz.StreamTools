using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.StreamLib.Core
{
	public interface IAttentionClient
	{
		Task AlertFritz();
	}
}
