using AppCore.Interfaces;
using AppCore.Models.Whitebit.Request;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class WhitebitAuthenticator : IWhitebitAuthenticator
    {
        private readonly string _apiSecret;
        private readonly string _apiKey;

        public WhitebitAuthenticator(string apiSecret, string apiKey)
        {
            _apiSecret = apiSecret;
            _apiKey = apiKey;
        }

        public HttpRequestMessage GetAuthenticatedRequest(BaseWhitebitRequest data)
        {
            var nonce = GetNonce();
            data.Nonce = nonce;

            var dataJson = JsonConvert.SerializeObject(data);
            var payload = Base64Encode(dataJson);
            var signature = CalculateSignature(payload, _apiSecret);

            var content = new StringContent(dataJson, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, data.Request)
            {
                Content = content
            };
            requestMessage.Headers.Add("X-TXC-APIKEY", _apiKey);
            requestMessage.Headers.Add("X-TXC-PAYLOAD", payload);
            requestMessage.Headers.Add("X-TXC-SIGNATURE", signature);

            return requestMessage;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        private static string GetNonce()
        {
            var milliseconds = (long)DateTime.Now.ToUniversalTime()
                .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .TotalMilliseconds;

            return milliseconds.ToString();
        }

        public static string CalculateSignature(string text, string apiSecret)
        {
            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(apiSecret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(text));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            }
        }

    }
}