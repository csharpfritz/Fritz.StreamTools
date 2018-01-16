using System.Linq;

namespace Fritz.StreamTools.Models
{
  public class FollowerGoalConfiguration {

    public string Caption { get; set; } = "Follower Goal";

    public int Goal { get; set; } = 1;

    public string EmptyBackgroundColor { get; set; }

    public string EmptyFontColor { get; set; }

    public string FillBackgroundColor { get; set; }

		public string[] FillBgColorArray { get {
				return FillBackgroundColor.Split(',');
		} }

    public string FillFontColor { get; set; }

    public string FillBackgroundColorBlend { get; set; }

		public double[] FillBgBlendArray {  get {
				return FillBackgroundColorBlend.Split(',').Select(x => double.Parse(x)).ToArray();
		} }

  }
}
