using AppCore.Models.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore.Interfaces
{
    public interface IPairsService
    {
        List<TradePair> GetTradePairs();
    }
}