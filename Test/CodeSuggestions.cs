using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Test
{

	public class CodeSuggestions
	{

		internal static readonly Regex FileNameRegex = new Regex(@"(?<projectName>^[\w.]+\/)?(?<folders>[\w.^\/]+\/)*(?<filename>[\w.]+$)");

		[Theory]
		[InlineData("ConsoleApp1/Program.cs", "ConsoleApp1")]
		[InlineData("ConsoleApp1/Test/Program.cs", "ConsoleApp1")]
		[InlineData("ConsoleApp1/Test/Test1/Program.cs", "ConsoleApp1")]
		public void LocateProjectName(string filename, string expectedProjectName) {

			var match = FileNameRegex.Match(filename);
			var projectName = match.Groups["projectName"].Success ? match.Groups["projectName"].Value.TrimEnd('/') : "";

			Assert.Equal(expectedProjectName, projectName);


		}

		[Fact]
		public void GivenNoProjectName() {

			var match = FileNameRegex.Match("Program.cs");
			var projectName = match.Groups["projectName"].Success ? match.Groups["projectName"].Value.TrimEnd('/') : "";

			Assert.Equal("", projectName);

		}

		[Theory]
		[InlineData("ConsoleApp1/Test/Program.cs", "Test")]
		[InlineData("ConsoleApp1/Test/Test1/Program.cs", "Test/Test1")]
		public void LocateFolderName(string filename, string expectedFolderNames)
		{

			var match = FileNameRegex.Match(filename);
			var folders = match.Groups["folders"].Success ? match.Groups["folders"].Value.TrimEnd('/') : "";

			Assert.Equal(expectedFolderNames, folders);


		}


	}
}
