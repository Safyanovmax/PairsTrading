using AppCore.Models.Binance.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Interfaces
{
    public interface IBinanceService
    {
        Task<GetBinanceOrderbookResponse> GetOrderbook(string market);

        Task<CreateBinanceMarketOrderResponse> CreateMarketOrder(string market, string side, decimal amount);

        Task<GetBinanceTradeBalancesResponse> GetTradeBalances();
    }
}