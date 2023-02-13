using AppCore.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Models.Trading
{
    public class TradePair
    {
        public TradeCurrencyType CryptoCurrency { get; set; }

        public TradeCurrencyType Currency { get; set; }

        public decimal CryptoAmount { get; set; }

        public decimal ActionDeviation { get; set; }
    }
}