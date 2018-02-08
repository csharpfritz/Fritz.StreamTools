using Fritz.StreamTools.Services;
using Xunit;
using FluentAssertions;

namespace StreamToolsTests.Services.StreamService
{


	public class CurrentFollowerCountShould
	{

		[Theory]
		[AutoMoqStreamServiceWithNameAndCount]
		public void MatchCurrentFollowerCount_GivenOneService(IStreamService jeffStreams)
		{


			// arrange
			var sut = new Fritz.StreamTools.Services.StreamService(new[] { jeffStreams });

			// act
			var totalFollowerCount = sut.CurrentFollowerCount;

			// assert
			totalFollowerCount
				.Should()
				.Be(jeffStreams.CurrentFollowerCount,
					because: "CurrentFollowerCount did not match");

		}

		[Theory]
		[AutoMoqStreamServiceWithNameAndCount]
		public void SumCountFromEachService_GivenMultipleServices(IStreamService jeffStreams, IStreamService otherStreamService)
		{


			// arrange
			var sut = new Fritz.StreamTools.Services.StreamService(new[] { jeffStreams, otherStreamService });

			// act
			var totalFollowerCount = sut.CurrentFollowerCount;

			// assert
			totalFollowerCount
				.Should()
				.Be(jeffStreams.CurrentFollowerCount + otherStreamService.CurrentFollowerCount,
					because: $"follower counts should be summed ");

		}

	}


}