using AppCore.Models.Whitebit.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Interfaces
{
    public interface IWhitebitService
    {
        Task<GetWhitebitOrderbookResponse> GetOrderbook(string market);

        Task<CreateWhitebitMarketOrderResponse> CreateMarketOrder(string market, string side, decimal amount);

        Task<GetWhitebitTradeBalancesResponse> GetTradeBalances();
    }
}