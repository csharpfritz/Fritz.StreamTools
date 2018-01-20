using Fritz.StreamTools.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Xunit;

namespace Test.Services.StreamService
{

	public class FollowerCountByService : BaseFixture
	{

		[Fact]
		public void ShouldCountSeparately()
		{

			// arrange
			var jeffStreams = Mockery.Create<IStreamService>();
			var jeffsFollowers = new Random().Next(10, 100);
			jeffStreams.SetupGet(j => j.CurrentFollowerCount).Returns(jeffsFollowers);
			jeffStreams.SetupGet(j => j.Name).Returns("Jeff");

			var otherStreamService = Mockery.Create<IStreamService>();
			var otherFollowers = new Random().Next(10, 100);
			otherStreamService.SetupGet(j => j.CurrentFollowerCount).Returns(otherFollowers);
			otherStreamService.SetupGet(j => j.Name).Returns("Other");

			// act
			var sut = new Fritz.StreamTools.Services.StreamService(new[] {
				jeffStreams.Object, otherStreamService.Object });
			var count = sut.FollowerCountByService;

			// assert
			Assert.Equal(2, count.Count());
			Assert.Contains(count, c => c.service == "Jeff");
			Assert.Contains(count, c => c.service == "Other");
			Assert.Equal(jeffsFollowers, count.First(c => c.service == "Jeff").count);
			Assert.Equal(otherFollowers, count.First(c => c.service == "Other").count);


		}

	}

}