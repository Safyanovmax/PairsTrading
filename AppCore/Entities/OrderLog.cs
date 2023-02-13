using AppCore.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Entities
{
    public class OrderLog : BaseEntity
    {
        public TradeExchangeType Exchange { get; set; }

        public TradeCurrencyType BaseCurrency { get; set; }

        public TradeCurrencyType QuoteCurrency { get; set; }

        public TradeOrderType Type { get; set; }

        public decimal Price { get; set; }

        //clean without fee
        public decimal Amount { get; set; }

        public decimal Total { get; set; }

        public PairsTradingLog PairsTradingLog { get; set; }

        public int PairsTradingLogId { get; set; }
    }
}