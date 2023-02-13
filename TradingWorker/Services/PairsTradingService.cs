using AppCore.Constants;
using AppCore.Entities;
using AppCore.Extensions;
using AppCore.Interfaces;
using AppCore.Models.Trading;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingWorker.Models;

namespace TradingWorker.Services
{
    public class PairsTradingService
    {
        private static Dictionary<TradeCurrencyType, PairsTradingCalculationData> _calculationDataCache;
        private readonly IWhitebitService _whitebitService;
        private readonly IBinanceService _binanceService;
        private readonly IPairsTradingCalculationService _calculationService;
        private readonly ITelegramNotificationService _telegramNotificationService;
        private readonly IPairsService _pairsService;
        private readonly IProfitCalculator _profitCalculator;
        private readonly AppDbContext _appDbContext;

        static PairsTradingService()
        {
            _calculationDataCache = new Dictionary<TradeCurrencyType, PairsTradingCalculationData>();
        }

        public PairsTradingService(IWhitebitService whitebitService,
            IBinanceService binanceService,
            ITelegramNotificationService telegramNotificationService,
            IPairsTradingCalculationService calculationService,
            IPairsService pairsService,
            IProfitCalculator profitCalculator,
            AppDbContext appDbContext)
        {
            _whitebitService = whitebitService;
            _binanceService = binanceService;
            _calculationService = calculationService;
            _telegramNotificationService = telegramNotificationService;
            _pairsService = pairsService;
            _profitCalculator = profitCalculator;
            _appDbContext = appDbContext;
        }

        public async Task Process()
        {
            var pairs = _pairsService.GetTradePairs();
            foreach (var pair in pairs)
            {
                await Process(pair);
                await Task.Delay(10000);
            }
        }

        //private async Task ProcessWithDelayOnTrade(TradePair pair)
        //{
        //    var isTradeTime = await Process(pair, afterDelay: false);
        //    if (isTradeTime)
        //    {
        //        await Task.Delay(5000);
        //    }
        //}

        private async Task Process(TradePair pair)
        {
            var cryptoCurrency = pair.CryptoCurrency;
            var currency = pair.Currency;
            var baseCryptoAmount = pair.CryptoAmount;

            PairsTradingCalculationData calculationData;
            if (!_calculationDataCache.TryGetValue(cryptoCurrency, out calculationData))
            {
                calculationData = _calculationService.Calculate(pair);
                _calculationDataCache.Add(cryptoCurrency, calculationData);
            }

            var tradingLog = _appDbContext.PairsTradingLogs
                .OrderByDescending(l => l.CreatedDate)
                .FirstOrDefault(l => l.Currency == currency
                && l.CryptoCurrency == cryptoCurrency);

            if (tradingLog == null)
            {
                tradingLog = new PairsTradingLog
                {
                    CreatedDate = DateTime.UtcNow,
                    CryptoCurrency = cryptoCurrency,
                    Currency = currency,
                    State = PairsTradingLogState.NoTrade
                };

                await _appDbContext.PairsTradingLogs.AddAsync(tradingLog);
                await _appDbContext.SaveChangesAsync();
            }

            var currentTradingLogNotification = $"Pair: {cryptoCurrency}/{currency}; State: {tradingLog.State}; ";
            if (tradingLog.State == PairsTradingLogState.TradeOpen)
            {
                currentTradingLogNotification = currentTradingLogNotification + $"LastZ: {tradingLog.LastZ}";
            }
            await _telegramNotificationService.Send(currentTradingLogNotification);

            var binanceMarket = GetBinancePair(cryptoCurrency, currency);
            var whitebitMarket = GetWhiteBITPair(cryptoCurrency, currency);

            var binanceOrderbook = await _binanceService.GetOrderbook(binanceMarket);
            var whitebitOrderbook = await _whitebitService.GetOrderbook(whitebitMarket);

            var binanceAvgBuyPrice = GetAveragePriceToFill(binanceOrderbook.Asks, baseCryptoAmount);
            var binanceAvgSellPrice = GetAveragePriceToFill(binanceOrderbook.Bids, baseCryptoAmount);

            var whitebitAvgBuyPrice = GetAveragePriceToFill(whitebitOrderbook.Asks, baseCryptoAmount);
            var whitebitAvgSellPrice = GetAveragePriceToFill(whitebitOrderbook.Bids, baseCryptoAmount);

            var binanceHigher = binanceAvgBuyPrice > whitebitAvgBuyPrice ? true : false;
            var binancePrice = binanceHigher ? binanceAvgSellPrice : binanceAvgBuyPrice;
            var whitebitPrice = binanceHigher ? whitebitAvgBuyPrice : whitebitAvgSellPrice;

            decimal ratioNow = binancePrice / whitebitPrice;
            decimal z = (ratioNow - calculationData.AverageRatio) / calculationData.StandardRatio;

            var shortBinance = false;
            var isTradeTime = false;

            var ratioCheckLog = new RatioCheckLog
            {
                CreatedDate = DateTime.UtcNow,
                FirstExchange = TradeExchangeType.Binance,
                FirstExchangePrice = binancePrice,
                SecondExchange = TradeExchangeType.WhiteBIT,
                SecondExchangePrice = whitebitPrice,
                DifferenceInPercents = Math.Abs(100 - ratioNow * 100)
            };
            await _appDbContext.RatioCheckLogs.AddAsync(ratioCheckLog);
            await _appDbContext.SaveChangesAsync();

            var ratioMessage = $"{cryptoCurrency}/{currency}; Ratio: {ratioNow}; Difference: {ratioCheckLog.DifferenceInPercents}%; Z: {z}; Binance: {binancePrice}{currency}; WhiteBIT: {whitebitPrice}{currency}; ";
            await _telegramNotificationService.Send(ratioMessage);


            var notification = string.Empty;
            if (tradingLog.State == PairsTradingLogState.NoTrade)
            {
                if (z > calculationData.ActionDeviation)
                {
                    // Signal that Stock One is relatively overvalued, so if there is no open trade, the strategy is to short stock1 and go long on stock2
                    shortBinance = true; // Trade is to short stock1
                    isTradeTime = true; // Trade will be executed next day
                    notification = $"{DateTime.UtcNow} ({cryptoCurrency} / {currency}): " +
                        $"Decision to short Binance ({binancePrice}) and go long on WhiteBIT ({whitebitPrice}), Ratio {ratioNow}";
                    Console.WriteLine(notification);
                }
                else if (z < -calculationData.ActionDeviation)
                {
                    // Signal that Stock One is relatively undervalued, so if there is no open trade, the strategy is to go long on stock1 and short stock2
                    shortBinance = false; // Trade is to go long on stock1
                    isTradeTime = true; // Trade will be executed next day
                    notification = $"{DateTime.UtcNow} ({cryptoCurrency} / {currency}): " +
                        $" Decision to go long on  Binance ({binancePrice}) and short on WhiteBIT ({whitebitPrice}), Ratio {ratioNow} ";
                    Console.WriteLine(notification);
                }
            }
            else
            {
                if (tradingLog.LastZ > 0 & z < 0 | tradingLog.LastZ < 0 & z > 0)
                {
                    isTradeTime = true;
                    string position = tradingLog.ShortBinance.Value ?
                        $"short on Binance ({binancePrice}) and long on WhiteBIT ({whitebitPrice})"
                        : $"long on Binance ({binancePrice}) and short on WhiteBIT ({whitebitPrice})";
                    notification = $"{DateTime.UtcNow}: Decision to close the open position: {position}!";
                    Console.WriteLine(notification);
                }
            }


            if (isTradeTime)
            {
                var balances = await GetBalances();
                OrderLog binanceOrder = null;
                OrderLog whitebitOrder = null;
                PairsTradingLog newTradingLog = null;
                if (tradingLog.State == PairsTradingLogState.NoTrade)
                {
                    if (shortBinance)
                    {
                        //sell binance
                        var binanceCryptoBalance = balances[TradeExchangeType.Binance][cryptoCurrency];
                        var availableSellAmount = GetAvailableSellAmount(binanceCryptoBalance, baseCryptoAmount);
                        var sellOrder = await _binanceService.CreateMarketOrder(binanceMarket, BinanceConstants.Sell,
                            availableSellAmount);
                        var sellOrderPrice = sellOrder.Fills.First().Price.StringToDecimal();
                        binanceOrder = new OrderLog
                        {
                            BaseCurrency = cryptoCurrency,
                            QuoteCurrency = currency,
                            Amount = availableSellAmount,
                            Price = sellOrderPrice,
                            CreatedDate = DateTime.UtcNow,
                            Exchange = TradeExchangeType.Binance,
                            Total = sellOrderPrice * availableSellAmount,
                            Type = TradeOrderType.Sell
                        };

                        //buy whitebit
                        var whitebitFiatBalance = balances[TradeExchangeType.WhiteBIT][currency];
                        var availableBuyAmount = GetAvailableBuyAmount(whitebitFiatBalance,
                            baseCryptoAmount,
                            whitebitPrice);
                        var buyOrder = await _whitebitService.CreateMarketOrder(whitebitMarket, WhiteBITConstants.Buy,
                            availableBuyAmount);

                        whitebitOrder = new OrderLog
                        {
                            BaseCurrency = cryptoCurrency,
                            QuoteCurrency = currency,
                            Price = buyOrder.DealMoney.StringToDecimal() / buyOrder.DealStock.StringToDecimal(),
                            Amount = buyOrder.DealStock.StringToDecimal(),
                            Total = buyOrder.DealMoney.StringToDecimal(),
                            CreatedDate = DateTime.UtcNow,
                            Exchange = TradeExchangeType.WhiteBIT,
                            Type = TradeOrderType.Buy
                        };
                    }
                    else
                    {
                        //sell whitebit
                        var whitebitCryptoBanace = balances[TradeExchangeType.WhiteBIT][cryptoCurrency];
                        var availableSellAmount = GetAvailableSellAmount(whitebitCryptoBanace, baseCryptoAmount);
                        var sellOrder = await _whitebitService.CreateMarketOrder(whitebitMarket, WhiteBITConstants.Sell,
                            availableSellAmount);
                        whitebitOrder = new OrderLog
                        {
                            BaseCurrency = cryptoCurrency,
                            QuoteCurrency = currency,
                            Price = sellOrder.DealMoney.StringToDecimal() / sellOrder.DealStock.StringToDecimal(),
                            Amount = sellOrder.DealStock.StringToDecimal(),
                            Total = sellOrder.DealMoney.StringToDecimal() - sellOrder.DealFee.StringToDecimal(),
                            CreatedDate = DateTime.UtcNow,
                            Exchange = TradeExchangeType.WhiteBIT,
                            Type = TradeOrderType.Sell
                        };

                        //buy binance
                        var binanceFiatBalance = balances[TradeExchangeType.Binance][currency];
                        var availableBuyAmount = GetAvailableBuyAmount(binanceFiatBalance,
                            baseCryptoAmount,
                            binancePrice);
                        var buyOrder = await _binanceService.CreateMarketOrder(binanceMarket, BinanceConstants.Buy,
                            availableBuyAmount);
                        var buyOrderPrice = buyOrder.Fills.First().Price.StringToDecimal();
                        binanceOrder = new OrderLog
                        {
                            BaseCurrency = cryptoCurrency,
                            QuoteCurrency = currency,
                            Amount = availableBuyAmount,
                            Price = buyOrderPrice,
                            CreatedDate = DateTime.UtcNow,
                            Exchange = TradeExchangeType.Binance,
                            Total = buyOrderPrice * availableBuyAmount,
                            Type = TradeOrderType.Buy
                        };
                    }

                    newTradingLog = new PairsTradingLog
                    {
                        ShortBinance = shortBinance,
                        CryptoCurrency = cryptoCurrency,
                        Currency = currency,
                        CreatedDate = DateTime.UtcNow,
                        State = PairsTradingLogState.TradeOpen,
                        LastZ = z,
                        Ratio = ratioNow
                    };
                }
                else
                {
                    if (tradingLog.ShortBinance.Value)
                    {
                        //sell whitebit
                        var whitebitCryptoBalance = balances[TradeExchangeType.WhiteBIT][cryptoCurrency];
                        var availableSellAmount = whitebitCryptoBalance.Amount - baseCryptoAmount;
                        var sellOrder = await _whitebitService.CreateMarketOrder(whitebitMarket, WhiteBITConstants.Sell,
                            availableSellAmount);
                        whitebitOrder = new OrderLog
                        {
                            BaseCurrency = cryptoCurrency,
                            QuoteCurrency = currency,
                            Price = sellOrder.DealMoney.StringToDecimal() / sellOrder.DealStock.StringToDecimal(),
                            Amount = sellOrder.DealStock.StringToDecimal(),
                            Total = sellOrder.DealMoney.StringToDecimal() - sellOrder.DealFee.StringToDecimal(),
                            CreatedDate = DateTime.UtcNow,
                            Exchange = TradeExchangeType.WhiteBIT,
                            Type = TradeOrderType.Sell
                        };


                        //buy binance
                        var binanceFiatBalance = balances[TradeExchangeType.Binance][currency];
                        var availableBuyAmount = GetAvailableBuyAmount(binanceFiatBalance, baseCryptoAmount,
                            binancePrice);
                        var buyOrder = await _binanceService.CreateMarketOrder(binanceMarket, BinanceConstants.Buy,
                            availableBuyAmount);
                        var buyOrderPrice = buyOrder.Fills.First().Price.StringToDecimal();
                        binanceOrder = new OrderLog
                        {
                            BaseCurrency = cryptoCurrency,
                            QuoteCurrency = currency,
                            Amount = availableBuyAmount,
                            Price = buyOrderPrice,
                            CreatedDate = DateTime.UtcNow,
                            Exchange = TradeExchangeType.Binance,
                            Total = buyOrderPrice * availableBuyAmount,
                            Type = TradeOrderType.Buy
                        };
                    }
                    else
                    {
                        //sell binance
                        var binanceCryptoBalance = balances[TradeExchangeType.Binance][cryptoCurrency];
                        var availableSellAmount = binanceCryptoBalance.Amount - baseCryptoAmount;
                        var sellOrder = await _binanceService.CreateMarketOrder(binanceMarket, BinanceConstants.Sell, availableSellAmount);
                        var buyOrderPrice = sellOrder.Fills.First().Price.StringToDecimal();
                        binanceOrder = new OrderLog
                        {
                            BaseCurrency = cryptoCurrency,
                            QuoteCurrency = currency,
                            Amount = availableSellAmount,
                            Price = buyOrderPrice,
                            CreatedDate = DateTime.UtcNow,
                            Exchange = TradeExchangeType.Binance,
                            Total = buyOrderPrice * availableSellAmount,
                            Type = TradeOrderType.Sell
                        };

                        //buy whitebit
                        var whitebitFiatBalance = balances[TradeExchangeType.WhiteBIT][currency];
                        var availableBuyAmount = GetAvailableBuyAmount(whitebitFiatBalance, baseCryptoAmount,
                            whitebitPrice);
                        var buyOrder = await _whitebitService.CreateMarketOrder(whitebitMarket, WhiteBITConstants.Buy, availableBuyAmount);
                        whitebitOrder = new OrderLog
                        {
                            BaseCurrency = cryptoCurrency,
                            QuoteCurrency = currency,
                            Price = buyOrder.DealMoney.StringToDecimal() / buyOrder.DealStock.StringToDecimal(),
                            Amount = buyOrder.DealStock.StringToDecimal(),
                            Total = buyOrder.DealMoney.StringToDecimal(),
                            CreatedDate = DateTime.UtcNow,
                            Exchange = TradeExchangeType.WhiteBIT,
                            Type = TradeOrderType.Buy
                        };
                    }

                    newTradingLog = new PairsTradingLog
                    {
                        CreatedDate = DateTime.UtcNow,
                        CryptoCurrency = cryptoCurrency,
                        Currency = currency,
                        State = PairsTradingLogState.NoTrade,
                    };
                }

                await _telegramNotificationService.Send(notification);
                var binanceOrderMessage = $"Binance {binanceOrder.Type} {binanceOrder.Amount}{cryptoCurrency} for {binanceOrder.Total}{currency}, Price: {binanceOrder.Price}{currency}";
                await _telegramNotificationService.Send(binanceOrderMessage);

                var whitebitOrderMessage = $"WhiteBIT {whitebitOrder.Type} {whitebitOrder.Amount}{cryptoCurrency} for {whitebitOrder.Total}{currency}, Price: {whitebitOrder.Price}{currency}";
                await _telegramNotificationService.Send(whitebitOrderMessage);

                newTradingLog.OrderLogs.AddRange(new[] { binanceOrder, whitebitOrder });
                await _appDbContext.PairsTradingLogs.AddAsync(newTradingLog);
                await _appDbContext.SaveChangesAsync();

                if (tradingLog.State == PairsTradingLogState.TradeOpen
                    && isTradeTime)
                {
                    var profit = await _profitCalculator.CalculateLastProfit(tradingLog.Currency, tradingLog.CryptoCurrency);
                    var profitNotification = $"Profit: {profit}{pair.Currency}";
                    await _telegramNotificationService.Send(profitNotification);
                }
            }
        }

        private decimal GetAvailableSellAmount(TradeBalance balance, decimal baseAmount)
        {
            if (balance.Amount < baseAmount)
                return balance.Amount;

            return baseAmount;
        }

        private decimal GetAvailableBuyAmount(TradeBalance balance, decimal baseAmount, decimal buyPrice)
        {
            if (balance.Amount / buyPrice < baseAmount)
            {
                return (balance.Amount / buyPrice) * 0.95m;
            }

            return baseAmount;
        }

        private decimal GetWhiteBITAvailableBuyAmount(TradeBalance balance, decimal baseAmount, decimal buyPrice)
        {
            var buyAmount = GetAvailableBuyAmount(balance, baseAmount, buyPrice);
            return buyAmount * buyPrice;
        }

        private string GetBinancePair(TradeCurrencyType baseCurrency, TradeCurrencyType quoteCurrency)
        {
            return $"{baseCurrency}{quoteCurrency}";
        }

        private string GetWhiteBITPair(TradeCurrencyType baseCurrency, TradeCurrencyType quoteCurrency)
        {
            return $"{baseCurrency}_{quoteCurrency}";
        }


        private decimal GetAveragePriceToFill(List<List<string>> orders, decimal cryptoAmount)
        {
            cryptoAmount *= 2;
            var countOfOrdersToFill = 0;
            var totalPrice = 0m;
            var totalAmount = 0m;
            foreach (var buyOrder in orders)
            {
                countOfOrdersToFill++;

                var price = buyOrder[0].StringToDecimal();
                var amount = buyOrder[1].StringToDecimal();
                totalPrice += price;
                totalAmount += amount;

                if (totalAmount >= cryptoAmount)
                {
                    break;
                }
            }

            var binanceAvgPrice = totalPrice / countOfOrdersToFill;
            return binanceAvgPrice;
        }

        private async Task<Dictionary<TradeExchangeType, Dictionary<TradeCurrencyType, TradeBalance>>> GetBalances()
        {
            var whitebitBalances = await _whitebitService.GetTradeBalances();

            var whitebitETHBalance = new TradeBalance
            {
                Amount = whitebitBalances.ETH.Available.StringToDecimal(),
                Currency = TradeCurrencyType.ETH,
                Exchange = TradeExchangeType.WhiteBIT
            };

            var whitebitUSDTBalance = new TradeBalance
            {
                Amount = whitebitBalances.USDT.Available.StringToDecimal(),
                Currency = TradeCurrencyType.USDT,
                Exchange = TradeExchangeType.WhiteBIT
            };

            var whitebitUAHBalance = new TradeBalance
            {
                Amount = whitebitBalances.UAH.Available.StringToDecimal(),
                Currency = TradeCurrencyType.UAH,
                Exchange = TradeExchangeType.WhiteBIT
            };

            var binanceBalances = await _binanceService.GetTradeBalances();

            var binanceETHBalanceResponse = binanceBalances.Balances.First(b => b.Asset == "ETH");
            var binanceETHBalance = new TradeBalance
            {
                Amount = binanceETHBalanceResponse.Free.StringToDecimal(),
                Currency = TradeCurrencyType.ETH,
                Exchange = TradeExchangeType.Binance
            };

            var binanceUSDTBalanceResponse = binanceBalances.Balances.First(b => b.Asset == "USDT");
            var binanceUSDTBalance = new TradeBalance
            {
                Amount = binanceUSDTBalanceResponse.Free.StringToDecimal(),
                Currency = TradeCurrencyType.USDT,
                Exchange = TradeExchangeType.Binance
            };

            var binanceUAHBalanceResponse = binanceBalances.Balances.First(b => b.Asset == "UAH");
            var binanceUAHBalance = new TradeBalance
            {
                Amount = binanceUAHBalanceResponse.Free.StringToDecimal(),
                Currency = TradeCurrencyType.UAH,
                Exchange = TradeExchangeType.Binance
            };

            return new Dictionary<TradeExchangeType, Dictionary<TradeCurrencyType, TradeBalance>>
            {
                {
                    TradeExchangeType.Binance, new Dictionary<TradeCurrencyType, TradeBalance>
                    {
                        { TradeCurrencyType.USDT, binanceUSDTBalance  },
                        { TradeCurrencyType.ETH, binanceETHBalance  },
                        { TradeCurrencyType.UAH, binanceUAHBalance  }
                    }
                },
                {
                    TradeExchangeType.WhiteBIT, new Dictionary<TradeCurrencyType, TradeBalance>
                    {
                        { TradeCurrencyType.USDT, whitebitUSDTBalance  },
                        { TradeCurrencyType.ETH, whitebitETHBalance  },
                        { TradeCurrencyType.UAH, whitebitUAHBalance  }
                    }

                }
            };
        }
    }
}