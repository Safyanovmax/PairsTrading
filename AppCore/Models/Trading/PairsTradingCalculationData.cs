using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Models.Trading
{
    public class PairsTradingCalculationData
    {
        public decimal AverageRatio { get; set; }

        public decimal StandardRatio { get; set; }

        public decimal ActionDeviation { get; set; }
    }
}