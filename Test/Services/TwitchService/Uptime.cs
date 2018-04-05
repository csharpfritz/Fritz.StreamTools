using Fritz.StreamTools.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Test.Services.TwitchService
{

	
	public class Uptime
	{

		[Fact]
		public void ShouldQueryService()
		{

			var sut = TwitchProxy.GetUptimeForStream("csharpfritz", "t7y5txan5q662t7zj7p3l4wlth8zhv");

			Assert.Null(sut);


		}

		[Fact]
		public void ShouldHandleTwitchFormat()
		{

			var demoOutput = @"{
   ""stream"": {

			""_id"": 23932774784,
      ""game"": ""BATMAN - The Telltale Series"",
      ""viewers"": 7254,
      ""video_height"": 720,
      ""average_fps"": 60,
      ""delay"": 0,
      ""created_at"": ""2016-12-14T22:49:56Z"",
      ""is_playlist"": false,
      ""preview"": {
				""small"": ""https://static-cdn.jtvnw.net/previews-ttv/live_user_dansgaming-80x45.jpg"",
         ""medium"": ""https://static-cdn.jtvnw.net/previews-ttv/live_user_dansgaming-320x180.jpg"",
         ""large"": ""https://static-cdn.jtvnw.net/previews-ttv/live_user_dansgaming-640x360.jpg"",
         ""template"": ""https://static-cdn.jtvnw.net/previews-ttv/live_user_dansgaming-{width}x{height}.jpg""

			},
      ""channel"": {
				""mature"": false,
         ""status"": ""Dan is Batman? - Telltale's Batman"",
         ""broadcaster_language"": ""en"",
         ""display_name"": ""DansGaming"",
         ""game"": ""BATMAN - The Telltale Series"",
         ""language"": ""en"",
         ""_id"": 7236692,
         ""name"": ""dansgaming"",
         ""created_at"": ""2009-07-15T03:02:41Z"",
         ""updated_at"": ""2016-12-15T01:33:58Z"",
         ""partner"": true,
         ""logo"": ""https://static-cdn.jtvnw.net/jtv_user_pictures/dansgaming-profile_image-76e4a4ab9388bc9c-300x300.png"",
         ""video_banner"": ""https://static-cdn.jtvnw.net/jtv_user_pictures/dansgaming-channel_offline_image-d3551503c24c08ad-1920x1080.png"",
         ""profile_banner"": ""https://static-cdn.jtvnw.net/jtv_user_pictures/dansgaming-profile_banner-4c2b8ece8cd010b4-480.jpeg"",
         ""profile_banner_background_color"": null,
         ""url"": ""https://www.twitch.tv/dansgaming"",
         ""views"": 63906830,
         ""followers"": 538598

			}
		}
	}";

			var sut = TwitchProxy.ParseStreamForCreatedAt(demoOutput);

			Assert.NotNull(sut);
			Assert.NotEqual(DateTime.Now, sut.Value);

		}

	}

}
