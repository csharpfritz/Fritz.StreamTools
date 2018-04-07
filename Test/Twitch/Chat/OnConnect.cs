using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fritz.Twitch;
using Test.Twitch.Proxy;
using Xunit;
using Xunit.Abstractions;

namespace Test.Twitch.Chat
{
	public class OnConnect
	{

		public OnConnect(ITestOutputHelper outputHelper)
		{
			this.OutputHelper = outputHelper;

			_Settings = new Fritz.Twitch.ConfigurationSettings
			{
				ChannelName = "csharpfritz",
				ClientId = "t7y5txan5q662t7zj7p3l4wlth8zhv",
				UserId = "96909659",
				ChatBotName = "fritzbot_",
				OAuthToken = "-MY TOKEN-"
			};
		}

		public ITestOutputHelper OutputHelper { get; }

		private ConfigurationSettings _Settings;


		[Fact(Skip ="Need OAuth Token")]
		public void ShouldWork()
		{

			var sut = new ChatClient(_Settings, new XUnitLogger(OutputHelper));
			sut.Init();

			Task.Delay(1000);

			// sut.WhisperMessage("Hello from your bot", "csharpfritz");

			Task.Delay(1000).GetAwaiter().GetResult();
			sut.Dispose();

		}

		[Fact]
		public void UserNameIdentificationWorks()
		{

			var sampleMessage = "@badges=broadcaster/1,subscriber/0,premium/1;color=#0000FF;display-name=csharpfritz;emotes=;id=b5d0d4c2-f4d1-416f-952d-62807bbf9084;mod=0;room-id=96909659;subscriber=1;tmi-sent-ts=1523285299657;turbo=0;user-id=96909659;user-type= :csharpfritz!csharpfritz@csharpfritz.tmi.twitch.tv PRIVMSG #csharpfritz :test";

			Assert.True(ChatClient.reUserName.Match(sampleMessage).Success);
			Assert.Equal(1, ChatClient.reUserName.Match(sampleMessage).Captures.Count);
			Assert.Equal("csharpfritz", ChatClient.reUserName.Match(sampleMessage).Groups[1].Value);

		}

		[Fact]
		public void MessageIdentificationWorks()
		{

			var sampleMessage = "@badges=broadcaster/1,subscriber/0,premium/1;color=#0000FF;display-name=csharpfritz;emotes=;id=b5d0d4c2-f4d1-416f-952d-62807bbf9084;mod=0;room-id=96909659;subscriber=1;tmi-sent-ts=1523285299657;turbo=0;user-id=96909659;user-type= :csharpfritz!csharpfritz@csharpfritz.tmi.twitch.tv PRIVMSG #csharpfritz :test";

			var sut = new ChatClient(_Settings, new XUnitLogger(OutputHelper));

			Assert.True(ChatClient.reChatMessage.Match(sampleMessage).Success);
			Assert.Equal(1, ChatClient.reChatMessage.Match(sampleMessage).Captures.Count);
			Assert.Equal("test", ChatClient.reChatMessage.Match(sampleMessage).Groups[1].Value);

		}

	}
}
