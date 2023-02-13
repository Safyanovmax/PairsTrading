using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Extensions
{
    public static class DecimalExtensions
    {
        public static string DecimalToString(this decimal num)
        {
            return num.ToString("G", CultureInfo.InvariantCulture);
        }

        public static decimal StringToDecimal(this string str)
        {
            return decimal.Parse(str, CultureInfo.InvariantCulture);
        }

        public static decimal GetPercent(this decimal num, decimal percent)
        {
            return (num / 100) * percent;
        }
    }
}