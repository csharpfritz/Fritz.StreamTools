using Fritz.StreamTools.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Xunit;

namespace Test.Services.StreamService
{

	public class ViewerCountByService : BaseFixture
	{

		[Fact]
		public void ShouldCountSeparately()
		{

			// arrange
			var jeffStreams = Mockery.Create<IStreamService>();
			var jeffsViewers = new Random().Next(10, 100);
			jeffStreams.SetupGet(j => j.CurrentViewerCount).Returns(jeffsViewers);
			jeffStreams.SetupGet(j => j.Name).Returns("Jeff");

			var otherStreamService = Mockery.Create<IStreamService>();
			var otherViewers = new Random().Next(10, 100);
			otherStreamService.SetupGet(j => j.CurrentViewerCount).Returns(otherViewers);
			otherStreamService.SetupGet(j => j.Name).Returns("Other");

			// act
			var sut = new Fritz.StreamTools.Services.StreamService(new[] {
				jeffStreams.Object, otherStreamService.Object });
			var count = sut.ViewerCountByService;

			// assert
			Assert.Equal(2, count.Count());
			Assert.Equal(jeffsViewers, count.First(c => c.service == "Jeff").count);
			Assert.Equal(otherViewers, count.First(c => c.service == "Other").count);


		}

	}

}