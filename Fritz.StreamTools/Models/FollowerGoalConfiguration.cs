using System.Linq;

namespace Fritz.StreamTools.Models
{
    public class FollowerGoalConfiguration
    {
        public string Caption { get; set; } = "Follower Goal";
        public int Goal { get; set; } = 1;
        public int CurrentValue { get; set; } = 0;
        public int Width { get; set; }
        public string EmptyBackgroundColor { get; set; }
        public string EmptyFontColor { get; set; }
        public string FillFontColor { get; set; }
        public string FontName { get; set; }
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
    }
}