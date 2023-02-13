using AppCore.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingWorker.Models
{
    public class TradeBalance
    {
        public decimal Amount { get; set; }

        public TradeExchangeType Exchange { get; set; }

        public TradeCurrencyType Currency { get; set; }
    }
}