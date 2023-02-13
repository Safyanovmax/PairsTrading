using AppCore.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingWorker.Models
{
    public class PairsTradingPrice
    {
        public TradeExchangeType Exchange { get; set; }

        public decimal Price { get; set; }
    }
}