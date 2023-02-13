using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Models.Whitebit.Response
{
    public class GetWhitebitOrderbookResponse
    {
        [JsonProperty("asks")]
        public List<List<string>> Asks { get; set; }

        [JsonProperty("bids")]
        public List<List<string>> Bids { get; set; }
    }
}
