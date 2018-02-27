using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Helpers
{
    public static class DisplayHelper
    {
        /// <summary>
        /// Produces the color gradient string required for linear-gradient based on a set of colors and the requested blending
        /// </summary>
        /// <param name="bgcolors">An array of valid CSS colors such as red,green,blue or #F00,#0F0,#00F</param>
        /// <param name="bgblend">An array of percentage blends required for each color - expressed between 0 and 1 </param>
        /// <param name="width">The total width to blend over</param>
        /// <returns></returns>
        public static string Gradient(string[] bgcolors, double[] bgblend, int width)
        {

            var count = (double)bgcolors.Length;

            // no colors
            if (count == 0) return "";
            // 1 color = no gradient
            if (count == 1) return $"{bgcolors[0]},{bgcolors[0]}";

            var colorWidth = (int)(width / (count - 1));

            var result = new StringBuilder();
            for (var c = 0; c < count - 1; c++)
            {
                var distance = c * colorWidth;

                // Each color has an anchor equidistant from the other colors
                if (result.Length > 0)
                {
                    result.Append($",{bgcolors[c]} {distance}px");
                }
                else
                {
                    result.Append($"{bgcolors[c]} {distance}px");
                }

                var blend = 1.0;

                if (bgblend != null && bgblend.Length > c)
                {
                    blend = bgblend[c];
                }

                // Mark the end of this color based on its blend %
                distance = (int)(c * colorWidth + (1 - blend) * colorWidth * 0.5);

                result.Append($",{bgcolors[c]} {distance}px");

                // Now add the start of the next color based on this blend %
                distance = (int)((c + 1) * colorWidth - (1 - blend) * colorWidth * 0.5);

                result.Append($",{bgcolors[c + 1]} {distance}px");
            }

            result.Append($", {bgcolors.Last()}");
            return result.ToString();
        }

    }
}
