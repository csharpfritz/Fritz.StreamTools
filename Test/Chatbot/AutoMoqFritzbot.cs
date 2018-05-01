using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;
using Fritz.StreamTools.Services;
using Microsoft.Extensions.Configuration;

namespace Test.Chatbot
{
	public class AutoMoqFritzbot : AutoDataAttribute
	{

		// This is really confusing...  creating an attribute that references a customization
		// that creates a definition for what a Mocked object should do


		//public AutoMoqFritzbot() : base(new Fixture().Customize(new CompositeCustomization(
		//	new CompositeCustomization(
		//			new AutoMoqConfigurationCustomization(),
		//			new AutoMoqCustomization()
		//	)))
		//{

		//}


	}

	internal class AutoMoqConfigurationCustomization : ICustomization
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

				if (type == null || type != typeof(IConfiguration))
				{
					return new NoSpecimen();
				}

				var fixture = new Fixture();
				var mock = new Moq.Mock<IConfiguration>();
				mock.SetupGet(s => s[FritzBot.CONFIGURATION_ROOT + ":CooldownTime"]).Returns(fixture.Create<TimeSpan>().ToString());

				return mock.Object;

			}
		}
	}
}
