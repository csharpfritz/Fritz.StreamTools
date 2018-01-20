using Fritz.StreamTools.Services;
using System.Linq;
using Xunit;

namespace Test.Services.StreamService
{

	public class FollowerCountByService
	{

		[Theory]
		[AutoMoqStreamServiceWithNameAndCount]
		public void ShouldCountSeparately(IStreamService jeffStreams, IStreamService otherStreamService)
		{

			// arrange

			// act
			var sut = new Fritz.StreamTools.Services.StreamService(new[] { jeffStreams, otherStreamService });
			var count = sut.FollowerCountByService.ToList();

			// assert
			Assert.Equal(2, count.Count());
			Assert.Contains(count, c => c.service == jeffStreams.Name);
			Assert.Contains(count, c => c.service == otherStreamService.Name);
			Assert.Equal(jeffStreams.CurrentFollowerCount, count.First(c => c.service == jeffStreams.Name).count);
			Assert.Equal(otherStreamService.CurrentFollowerCount, count.First(c => c.service == otherStreamService.Name).count);


		}

	}

}
