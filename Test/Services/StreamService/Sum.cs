using Fritz.StreamLib.Core;
using Fritz.StreamTools.Services;
using Xunit;

namespace Test.Services.StreamService
{


	public class Sum
	{

		[Theory]
		[AutoMoqStreamServiceWithNameAndCount]
		public void CurrentFollowerCountHandlesOneService(IStreamService jeffStreams)
		{


			// arrange

			// act
			var sut = new Fritz.StreamTools.Services.StreamService(new[] { jeffStreams });
			var sum = sut.CurrentFollowerCount;

			// assert
			Assert.Equal(jeffStreams.CurrentFollowerCount, sum);

		}

		[Theory]
		[AutoMoqStreamServiceWithNameAndCount]
		public void CurrentFollowerCountHandlesMultipleServices(IStreamService jeffStreams, IStreamService otherStreamService)
		{


			// arrange

			// act
			var sut = new Fritz.StreamTools.Services.StreamService(new[] { jeffStreams, otherStreamService });
			var sum = sut.CurrentFollowerCount;

			// assert
			Assert.Equal(jeffStreams.CurrentFollowerCount + otherStreamService.CurrentFollowerCount, sum);

		}

	}


}
