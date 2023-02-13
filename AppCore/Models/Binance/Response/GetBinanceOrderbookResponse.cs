using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Models.Binance.Response
{
    public class GetBinanceOrderbookResponse
    {
        [JsonProperty("bids")]
        public List<List<string>> Bids { get; set; }

        [JsonProperty("asks")]
        public List<List<string>> Asks { get; set; }
    }
}