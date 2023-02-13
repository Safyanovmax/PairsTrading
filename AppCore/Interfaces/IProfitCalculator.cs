using AppCore.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Interfaces
{
    public interface IProfitCalculator
    {
        Task<decimal> CalculateLastProfit(TradeCurrencyType currency, TradeCurrencyType cryptoCurrency);
    }
}