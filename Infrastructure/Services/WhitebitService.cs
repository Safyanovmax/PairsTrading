using AppCore.Constants;
using AppCore.Extensions;
using AppCore.Interfaces;
using AppCore.Models.Whitebit.Request;
using AppCore.Models.Whitebit.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class WhitebitService : IWhitebitService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWhitebitAuthenticator _authenticator;

        public WhitebitService(IHttpClientFactory httpClientFactory, IWhitebitAuthenticator authenticator)
        {
            _httpClientFactory = httpClientFactory;
            _authenticator = authenticator;
        }

        public async Task<GetWhitebitOrderbookResponse> GetOrderbook(string market)
        {
            var httpClient = _httpClientFactory.CreateClient(TradeExchangeType.WhiteBIT.ToString());
            var url = $"/api/v4/public/orderbook/{market}?limit=50";
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var resultStr = await response.Content.ReadAsStringAsync();
            var orderbook = JsonConvert.DeserializeObject<GetWhitebitOrderbookResponse>(resultStr);
            return orderbook;
        }

        public async Task<CreateWhitebitMarketOrderResponse> CreateMarketOrder(string market, string side, decimal amount)
        {
            var httpClient = _httpClientFactory.CreateClient(TradeExchangeType.WhiteBIT.ToString());
            var url = "/api/v4/order/stock_market";

            var request = new CreateMarketOrderRequest
            {
                Market = market,
                Side = side,
                Request = url,
                Amount = amount.DecimalToString()
            };
            var requestMessage = _authenticator.GetAuthenticatedRequest(request);

            var responseMessage = await httpClient.SendAsync(requestMessage);
            var responseBody = await responseMessage.Content.ReadAsStringAsync();
            responseMessage.EnsureSuccessStatusCode();

            var response = JsonConvert.DeserializeObject<CreateWhitebitMarketOrderResponse>(responseBody);

            return response;
        }

        public async Task<GetWhitebitTradeBalancesResponse> GetTradeBalances()
        {
            var httpClient = _httpClientFactory.CreateClient(TradeExchangeType.WhiteBIT.ToString());
            var url = "/api/v4/trade-account/balance";

            var request = new BaseWhitebitRequest
            {
                Request = url,
            };
            var requestMessage = _authenticator.GetAuthenticatedRequest(request);

            var responseMessage = await httpClient.SendAsync(requestMessage);
            responseMessage.EnsureSuccessStatusCode();

            var responseBody = await responseMessage.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<GetWhitebitTradeBalancesResponse>(responseBody);

            return response;
        }
    }
}