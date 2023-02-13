using AppCore.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Entities
{
    public class RatioCheckLog : BaseEntity
    {
        public decimal DifferenceInPercents { get; set; }

        public decimal FirstExchangePrice { get; set; }

        public TradeExchangeType FirstExchange { get; set; }

        public decimal SecondExchangePrice { get; set; }

        public TradeExchangeType SecondExchange { get; set; }
    }
}