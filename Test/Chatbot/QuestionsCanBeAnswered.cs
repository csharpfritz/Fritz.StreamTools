using Fritz.Chatbot.Commands;
using Octokit;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace Test.Chatbot
{
	public class QuestionsCanBeAnswered
	{

		public const string UserName = "TestUser";

		[Theory]
		[InlineData("What is this music?")]
		[InlineData("Hey @csharpfritz, What is this music?")]
		[InlineData("Hey @thefritzbot, What is this music?")]
		public void ShouldBeAnswered(string test)
		{

			var sut = new AzureQnACommand(null, null, null);

			var result = sut.CanExecute(UserName, test);
			Assert.True(result);

		}

		[Theory]
		[InlineData("I like turtles")]
		[InlineData("Wassup?")]
		[InlineData("Hey @somebodyelse, did you see the game this weekend?")]
		[InlineData("Hey @csharpfritzfoobar, did you see the game this weekend?")]
		public void ShouldNotBeAnswered(string test)
		{

			var sut = new AzureQnACommand(null, null, null);

			var result = sut.CanExecute(UserName, test);
			Assert.False(result);

		}

	}
}
