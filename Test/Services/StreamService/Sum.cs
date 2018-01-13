using Fritz.StreamTools.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Test.Services.StreamService
{


	public class Sum : BaseFixture
	{

		[Fact]
		public void CurrentFollowerCountHandlesOneService()
		{


			// arrange
			var jeffStreams = Mockery.Create<IStreamService>();
			var currentFollowers = new Random().Next(10, 100);
			jeffStreams.SetupGet(j => j.CurrentFollowerCount).Returns(currentFollowers);

			// act
			var sut = new Fritz.StreamTools.Services.StreamService(new[] { jeffStreams.Object });
			var sum = sut.CurrentFollowerCount;

			// assert
			Assert.Equal(currentFollowers, sum);

		}

		[Fact]
		public void CurrentFollowerCountHandlesMultipleServices()
		{


			// arrange
			var jeffStreams = Mockery.Create<IStreamService>();
			var otherStreamService = Mockery.Create<IStreamService>();
			var jeffsFollowers = new Random().Next(10, 100);
			jeffStreams.SetupGet(j => j.CurrentFollowerCount).Returns(jeffsFollowers);
			var otherFollowers = new Random().Next(10, 100);
			otherStreamService.SetupGet(j => j.CurrentFollowerCount).Returns(otherFollowers);

			// act
			var sut = new Fritz.StreamTools.Services.StreamService(new[] { jeffStreams.Object, otherStreamService.Object });
			var sum = sut.CurrentFollowerCount;

			// assert
			Assert.Equal((jeffsFollowers + otherFollowers), sum);

		}

	}


}
