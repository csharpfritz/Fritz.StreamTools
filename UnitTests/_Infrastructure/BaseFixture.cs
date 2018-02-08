using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamToolsTests
{


	public abstract class BaseFixture
	{

		public BaseFixture()
		{
			Mockery = new MockRepository(MockBehavior.Loose);
		}

		protected MockRepository Mockery { get; private set; }

	}

}