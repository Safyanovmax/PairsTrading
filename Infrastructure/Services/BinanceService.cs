using AppCore.Constants;
using AppCore.Extensions;
using AppCore.Interfaces;
using AppCore.Models.Binance.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class BinanceService : IBinanceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBinanceAuthenticator _authenticator;
        public BinanceService(IHttpClientFactory httpClientFactory,
            IBinanceAuthenticator authenticator)
        {
            _httpClientFactory = httpClientFactory;
            _authenticator = authenticator;
        }

        public async Task<CreateBinanceMarketOrderResponse> CreateMarketOrder(string market, string side, decimal amount)
        {
            var httpClient = _httpClientFactory.CreateClient(TradeExchangeType.Binance.ToString());
            var url = "/api/v3/order";
            var parameters = new Dictionary<string, object>
            {
                { "symbol", market },
                { "side", side },
                { "type", "MARKET" },
                { "quantity", amount.DecimalToString() }
            };

            var requestMessage = _authenticator.GetAuthenticatedRequest(url, parameters, HttpMethod.Post);
            var response = await httpClient.SendAsync(requestMessage);
            var resultStr = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var createMarketOrderResponse = JsonConvert.DeserializeObject<CreateBinanceMarketOrderResponse>(resultStr);
            return createMarketOrderResponse;
            throw new Exception();
        }

        public async Task<GetBinanceOrderbookResponse> GetOrderbook(string market)
        {
            var httpClient = _httpClientFactory.CreateClient(TradeExchangeType.Binance.ToString());
            var url = $"api/v3/depth?symbol={market}&limit=50";
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var resultStr = await response.Content.ReadAsStringAsync();
            var getOrderbookResponse = JsonConvert.DeserializeObject<GetBinanceOrderbookResponse>(resultStr);
            return getOrderbookResponse;
        }

        public async Task<GetBinanceTradeBalancesResponse> GetTradeBalances()
        {
            var httpClient = _httpClientFactory.CreateClient(TradeExchangeType.Binance.ToString());
            var url = "/api/v3/account";
            var parameters = new Dictionary<string, object>();
            var requestMessage = _authenticator.GetAuthenticatedRequest(url, parameters, HttpMethod.Get);

            var response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var resultStr = await response.Content.ReadAsStringAsync();
            var getTradeBalancesResponse = JsonConvert.DeserializeObject<GetBinanceTradeBalancesResponse>(resultStr);
            return getTradeBalancesResponse;
        }
    }
}