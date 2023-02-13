using AppCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Infrastructure.Services
{
    public class BinanceAuthenticator : IBinanceAuthenticator
    {
        private readonly string _apiSecret;
        private readonly string _apiKey;

        public BinanceAuthenticator(string apiSecret, string apiKey)
        {
            _apiSecret = apiSecret;
            _apiKey = apiKey;
        }

        public HttpRequestMessage GetAuthenticatedRequest(string url,
            Dictionary<string, object> parameters, HttpMethod method)
        {
            parameters.Add("timestamp", GenerateTimestamp());
            StringBuilder queryStringBuilder = new StringBuilder();
            queryStringBuilder = this.BuildQueryString(parameters, queryStringBuilder);
            string signature = GenerateSignature(queryStringBuilder.ToString());

            if (queryStringBuilder.Length > 0)
            {
                queryStringBuilder.Append("&");
            }

            queryStringBuilder.Append("signature=").Append(signature);
            url += "?" + queryStringBuilder.ToString();

            var requestMessage = new HttpRequestMessage(method, url);
            requestMessage.Headers.Add("X-MBX-APIKEY", _apiKey);
            return requestMessage;
        }

        private StringBuilder BuildQueryString(Dictionary<string, object> queryParameters, StringBuilder builder)
        {
            foreach (KeyValuePair<string, object> queryParameter in queryParameters)
            {
                string queryParameterValue = Convert.ToString(queryParameter.Value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(queryParameterValue))
                {
                    if (builder.Length > 0)
                    {
                        builder.Append("&");
                    }

                    builder
                        .Append(queryParameter.Key)
                        .Append("=")
                        .Append(HttpUtility.UrlEncode(queryParameterValue));
                }
            }

            return builder;
        }

        private long GenerateTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private string GenerateSignature(string parameters)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(_apiSecret);
            using (HMACSHA256 hmacsha256 = new HMACSHA256(keyBytes))
            {
                byte[] sourceBytes = Encoding.UTF8.GetBytes(parameters);

                byte[] hash = hmacsha256.ComputeHash(sourceBytes);

                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            }
        }
    }
}
