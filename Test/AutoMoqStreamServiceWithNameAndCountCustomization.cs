using System;
using AutoFixture;
using AutoFixture.Kernel;
using Fritz.StreamLib.Core;
using Fritz.StreamTools.Services;
using Moq;

namespace Test
{

	public class AutoMoqStreamServiceWithNameAndCountCustomization : ICustomization
	{

		public void Customize(IFixture fixture)
		{

			fixture.Customizations.Add(new SpecimenBuilder());

		}

		private class SpecimenBuilder : ISpecimenBuilder
		{

			public object Create(object request, ISpecimenContext context)
			{

				var type = request as Type;

				if (type == null || type != typeof(IStreamService))
				{
					return new NoSpecimen();
				}

				var fixture = new Fixture();
				var mock = new Mock<IStreamService>();
				mock.SetupGet(s => s.CurrentFollowerCount).Returns(fixture.Create<int>());
				mock.SetupGet(s => s.CurrentViewerCount).Returns(fixture.Create<int>());
				mock.SetupGet(s => s.Name).Returns(fixture.Create<string>());

				return mock.Object;

			}

		}

	}

}
