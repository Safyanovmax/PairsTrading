using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Models.Whitebit.Response
{
    public class CreateWhitebitMarketOrderResponse
    {
        [JsonProperty("orderId")]
        public long OrderId { get; set; }

        [JsonProperty("clientOrderId")]
        public string ClientOrderId { get; set; }

        [JsonProperty("market")]
        public string Market { get; set; }

        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("timestamp")]
        public double Timestamp { get; set; }

        [JsonProperty("dealMoney")]
        public string DealMoney { get; set; }

        [JsonProperty("dealStock")]
        public string DealStock { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("takerFee")]
        public string TakerFee { get; set; }

        [JsonProperty("makerFee")]
        public string MakerFee { get; set; }

        [JsonProperty("left")]
        public string Left { get; set; }

        [JsonProperty("dealFee")]
        public string DealFee { get; set; }
    }
}