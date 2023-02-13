using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Models.Whitebit.Request
{
    public class BaseWhitebitRequest
    {

        [JsonProperty("request")]
        public string Request { get; set; }

        [JsonProperty("nonce")]
        public string Nonce { get; set; }

        [JsonProperty("nonceWindow")]
        public bool NonceWindow { get; set; } = true;
    }
}
