using AppCore.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Entities
{
    public class PairsTradingLog : BaseEntity
    {
        public decimal? Ratio { get; set; }

        public decimal? LastZ { get; set; }

        public bool? ShortBinance { get; set; }

        public PairsTradingLogState State { get; set; }

        public TradeCurrencyType Currency { get; set; }

        public TradeCurrencyType CryptoCurrency { get; set; }

        public List<OrderLog> OrderLogs { get; set; } = new List<OrderLog>();
    }
}