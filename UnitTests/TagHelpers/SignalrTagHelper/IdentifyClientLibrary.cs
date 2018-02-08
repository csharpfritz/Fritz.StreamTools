using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;
using Xunit;
using WEB = Fritz.StreamTools;

namespace StreamToolsTests.TagHelpers.SignalrTagHelper
{

	public class IdentifyClientLibrary : BaseFixture
	{

		[Fact]
		public void WhenClientLibraryFoundShouldOutput() {

			// arrange
			var hostingEnvironment = Mockery.Create<IHostingEnvironment>();
			var fileSystem = Mockery.Create<IFileSystem>();

			// act
			//var sut = new WEB.TagHelpers.SignalrTagHelper(hostingEnvironment.Object);
			// var result = sut.IdentifyClientLibrary(fileSystem.Object, "TESTPATH");

			// assert


		}

	}

}
