using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Test
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