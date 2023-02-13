using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Models.Binance.Response
{
    public class GetBinanceTradeBalancesResponse
    {
        [JsonProperty("balances")]
        public List<GetBinanceTradeBalancesResponseItem> Balances { get; set; }
    }

    public class GetBinanceTradeBalancesResponseItem
    {
        [JsonProperty("asset")]
        public string Asset { get; set; }

        [JsonProperty("free")]
        public string Free { get; set; }

        [JsonProperty("locked")]
        public string Locked { get; set; }
    }
}