using Fritz.StreamTools.Services;
using System.Linq;
using Xunit;

namespace Test.Services.StreamService
{

	public class ViewerCountByService
	{

		[Theory]
		[AutoMoqStreamServiceWithNameAndCount]

		public void ShouldCountSeparately(IStreamService jeffStreams, IStreamService otherStreamService)
		{

			// arrange

			// act
			var sut = new Fritz.StreamTools.Services.StreamService(new[] { jeffStreams, otherStreamService });
			var count = sut.ViewerCountByService.ToList();

			// assert
			Assert.Equal(2, count.Count());
			Assert.Equal(jeffStreams.CurrentViewerCount, count.First(c => c.service == jeffStreams.Name).count);
			Assert.Equal(otherStreamService.CurrentViewerCount, count.First(c => c.service == otherStreamService.Name).count);


		}

	}

}
