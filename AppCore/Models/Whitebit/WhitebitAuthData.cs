using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Models.Whitebit
{
    public class WhitebitAuthData
    {
        public string ApiKey { get; set; }

        public string Payload { get; set; }

        public string Signature { get; set; }
    }
}