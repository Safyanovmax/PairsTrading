using AppCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class TelegramNotificationService : ITelegramNotificationService
    {
        private readonly string _secretKey = "";
        private readonly string _chatId = "";
        private readonly IHttpClientFactory _httpClientFactory;

        public TelegramNotificationService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task Send(string message)
        {
            var httpClient = _httpClientFactory.CreateClient("Telegram");
            var url = $"/{_secretKey}/sendMessage?chat_id={_chatId}&text={message}";
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }
    }
}