using Microsoft.UI;
using System;
using Windows.UI;

namespace Financial_Manager.Client.Helpers
{
    public static class ColorExtensions
    {
        public static Color FromHex(string hex)
        {
            hex = hex.Replace("#", string.Empty);

            byte a = 255;
            byte r = 0;
            byte g = 0;
            byte b = 0;

            if (hex.Length == 8)
            {
                a = Convert.ToByte(hex.Substring(0, 2), 16);
                r = Convert.ToByte(hex.Substring(2, 2), 16);
                g = Convert.ToByte(hex.Substring(4, 2), 16);
                b = Convert.ToByte(hex.Substring(6, 2), 16);
            }
            else if (hex.Length == 6)
            {
                r = Convert.ToByte(hex.Substring(0, 2), 16);
                g = Convert.ToByte(hex.Substring(2, 2), 16);
                b = Convert.ToByte(hex.Substring(4, 2), 16);
            }

            return ColorHelper.FromArgb(a, r, g, b);
        }
    }
}
