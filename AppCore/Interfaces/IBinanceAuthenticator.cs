using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Interfaces
{
    public interface IBinanceAuthenticator
    {
        HttpRequestMessage GetAuthenticatedRequest(string url,
            Dictionary<string, object> parameters, HttpMethod method);
    }
}
