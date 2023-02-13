using AppCore.Interfaces;
using AppCore.Models.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingWorker.Services
{
    public class PairsService : IPairsService
    {
        private readonly IConfiguration _configuration;

        public PairsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<TradePair> GetTradePairs()
        {
            var tradePairs = _configuration.GetSection("TradePairs").Get<List<TradePair>>();
            return tradePairs;
        }
    }
}