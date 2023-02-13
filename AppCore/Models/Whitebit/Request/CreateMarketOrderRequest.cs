using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Models.Whitebit.Request
{
    public class CreateMarketOrderRequest : BaseWhitebitRequest
    {
        [JsonProperty("market")]
        public string Market { get; set; }

        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        //[JsonProperty("clientOrderId")]
        //public string ClientOrderId { get; set; }
    }
}