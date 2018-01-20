using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace Test
{

	public class AutoMoqStreamServiceWithNameAndCountAttribute : AutoDataAttribute
	{

		public AutoMoqStreamServiceWithNameAndCountAttribute()
			: base(() => new Fixture().Customize(
				new CompositeCustomization(
					new AutoMoqStreamServiceWithNameAndCountCustomization(),
					new AutoMoqCustomization())))
		{
		}

	}

}