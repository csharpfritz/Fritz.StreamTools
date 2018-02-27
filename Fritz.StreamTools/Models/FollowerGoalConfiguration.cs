using Fritz.StreamTools.Helpers;
using System;
using System.Linq;

namespace Fritz.StreamTools.Models
{
		public class FollowerGoalConfiguration
		{
				public string Caption { get; set; } = "Follower Goal";
				public int Goal { get; set; } = 0;
				public int CurrentValue { get; set; } = 0;
				public int Width { get; set; } = 800;
				public string EmptyBackgroundColor { get; set; }
				public string EmptyFontColor { get; set; }
				public string FillFontColor { get; set; }
				public string FontName { get; set; } = "~";
				public string Gradient { get; set; }
				public string FillBackgroundColor { get; set; } = "";
				public string[] FillBgColorArray
				{
						get
						{
								return FillBackgroundColor.Split(',');
						}
				}
				public string FillBackgroundColorBlend { get; set; } = "0";
				public double[] FillBgBlendArray
				{
						get
						{
								return FillBackgroundColorBlend.Split(',').Select(x => double.Parse(x)).ToArray();
						}
				}

		internal void LoadDefaultValues(FollowerGoalConfiguration configuration)
		{

			this.Caption = string.IsNullOrEmpty(this.Caption) ? configuration.Caption : this.Caption == "null" ? "" : this.Caption;
			this.Goal = this.Goal == 0 ? configuration.Goal : this.Goal;
			var backColors = string.IsNullOrEmpty(this.FillBackgroundColor) ? configuration.FillBgColorArray : this.FillBackgroundColor.Split(',');
			var backBlend = string.IsNullOrEmpty(this.FillBackgroundColorBlend) ? configuration.FillBgBlendArray : this.FillBackgroundColorBlend.Split(',').Select(a => double.Parse(a)).ToArray();

			this.EmptyBackgroundColor = string.IsNullOrWhiteSpace(this.EmptyBackgroundColor) ? configuration.EmptyBackgroundColor : this.EmptyBackgroundColor;
			this.EmptyFontColor = string.IsNullOrWhiteSpace(this.EmptyFontColor) ? configuration.EmptyFontColor : this.EmptyFontColor;
			this.FontName = this.FontName == "~" ? configuration.FontName : this.FontName;

			this.Gradient = DisplayHelper.Gradient(backColors, backBlend, this.Width);


		}
	}
}