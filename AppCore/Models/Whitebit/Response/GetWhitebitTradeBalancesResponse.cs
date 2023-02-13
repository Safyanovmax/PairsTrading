using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Models.Whitebit.Response
{
    public class GetWhitebitTradeBalancesResponse
    {
        public GetTradeBalancesResponseItem USDT { get; set; }

        public GetTradeBalancesResponseItem UAH { get; set; }

        public GetTradeBalancesResponseItem ETH { get; set; }

        public GetTradeBalancesResponseItem BTC { get; set; }
    }

    public class GetTradeBalancesResponseItem
    {
        [JsonProperty("available")]
        public string Available { get; set; }

        [JsonProperty("freeze")]
        public string Freeze { get; set; }
    }
}