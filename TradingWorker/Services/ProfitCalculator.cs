using AppCore.Constants;
using AppCore.Entities;
using AppCore.Extensions;
using AppCore.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingWorker.Services
{
    public class ProfitCalculator : IProfitCalculator
    {
        private readonly AppDbContext _appDbContext;

        public ProfitCalculator(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        //public async Task<decimal> CalculatePotentialProfit(PairsTradingLog log, )

        public async Task<decimal> CalculateLastProfit(TradeCurrencyType currency, TradeCurrencyType cryptoCurrency)
        {
            var lastTradingLogs = await _appDbContext.PairsTradingLogs
                .Include(l => l.OrderLogs)
                .Where(l => l.Currency == currency
                    && l.CryptoCurrency == cryptoCurrency)
                .OrderByDescending(l => l.CreatedDate)
                .Take(2)
                .ToListAsync();

            var lastOpenLog = lastTradingLogs.First(l => l.State == PairsTradingLogState.TradeOpen);
            var lastCloseLog = lastTradingLogs.First(l => l.State == PairsTradingLogState.NoTrade);

            if (lastCloseLog.CreatedDate < lastOpenLog.CreatedDate)
                throw new InvalidOperationException("Invalid calculation data");

            var openShortTrade = lastOpenLog.OrderLogs.First(l => l.Type == TradeOrderType.Sell);
            var openLongTrade = lastOpenLog.OrderLogs.First(l => l.Type == TradeOrderType.Buy);

            var closeShortTrade = lastCloseLog.OrderLogs.First(l => l.Type == TradeOrderType.Buy);
            var closeLongTrade = lastCloseLog.OrderLogs.First(l => l.Type == TradeOrderType.Sell);

            var shortFee = GetFee(openShortTrade);
            var longFee = GetFee(openLongTrade);

            var shortProfit =
                openShortTrade.Total - openShortTrade.Total.GetPercent(shortFee)
                - (closeShortTrade.Total + closeShortTrade.Total.GetPercent(shortFee));

            var longProfit =
               -1m * (openLongTrade.Total + openLongTrade.Total.GetPercent(longFee))
               + (closeLongTrade.Total - closeLongTrade.Total.GetPercent(longFee));

            return longProfit + shortProfit;

        }

        private decimal GetFee(OrderLog order)
        {
            if (order.Exchange == TradeExchangeType.Binance)
                return BinanceConstants.SpotFee;

            return WhiteBITConstants.SpotFee;
        }
    }
}