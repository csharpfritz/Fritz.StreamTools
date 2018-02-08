using Fritz.StreamTools.Services;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace StreamToolsTests.Services.StreamService
{

	public class FollowerCountByServiceShould
	{

		[Theory]
		[AutoMoqStreamServiceWithNameAndCount]
		public void CountSeparately(IStreamService jeffStreams, IStreamService otherStreamService)
		{

			// arrange

			// act
			var sut = new Fritz.StreamTools.Services.StreamService(new[] { jeffStreams, otherStreamService });
			var serviceList = sut.FollowerCountByService.ToList();

			// assert
			Assert.Equal(2, serviceList.Count());
			Assert.Contains(serviceList, c => c.service == jeffStreams.Name);
			Assert.Contains(serviceList, c => c.service == otherStreamService.Name);
			Assert.Equal(jeffStreams.CurrentFollowerCount, serviceList.First(c => c.service == jeffStreams.Name).count);
			Assert.Equal(otherStreamService.CurrentFollowerCount, serviceList.First(c => c.service == otherStreamService.Name).count);

		}

	}

}