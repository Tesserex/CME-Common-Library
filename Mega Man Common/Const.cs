using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.ComponentModel;

namespace MegaMan
{
    public static class Const
    {
        public static readonly NumberFormatInfo NumberFormat = new NumberFormatInfo()
        {
            NumberDecimalSeparator = "."
        };

        public static bool TryParse(this string s, out int result)
        {
            return int.TryParse(s, NumberStyles.Integer, Const.NumberFormat, out result);
        }

        public static bool TryParse(this string s, out float result)
        {
            return float.TryParse(s, NumberStyles.Float, Const.NumberFormat, out result);
        }

        public static bool TryParse(this string s, out double result)
        {
            return double.TryParse(s, NumberStyles.Float, Const.NumberFormat, out result);
        }
    }
}
