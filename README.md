# Introduction
**Pairs trade** is a trading strategy that involves matching a long position with a short position in two stocks with a high correlation.

**Key takeaways:**

- A pairs trade is a trading strategy that involves matching a long position with a short position in two stocks with a high correlation.
- Pairs trading was first introduced in the mid-1980s by a group of technical analyst researchers.

A pairs trade strategy is based on the historical correlation of two securities; the securities in a pairs trade must have a high positive correlation, which is the primary driver behind the strategy’s profits.
## **Example of Pairs Trade:**
To illustrate the potential profit of the pairs trade strategy, consider Stock A and Stock B, which have a high correlation of 0.95. The two stocks deviate from their historical trending correlation in the short-term, with a correlation of 0.50.

The arbitrage trader steps in to take a dollar matched the long position on underperforming Stock A and a short position on outperforming Stock B. The stocks converge and return to their 0.95 correlation over time. The trader profits from a long position and closed short position.

# Investigation
I made an assumption that pairs trading strategy can be applied to the same crypto pairs, but on different trade exchanges. 

For researching purposes I used two different exchanges:

- **Binance (**https://www.binance.com/**)**: global company that operates the largest cryptocurrency exchange in the world in terms of daily trading volume of cryptocurrencies.
- **WhiteBIT (**https://whitebit.com/**):** the largest European crypto-to-fiat exchange from Ukraine

Crypto-to-crypto pairs on these two different exchanges have high correlation, but they don’t have any divergence and convergence. It means their rates are always almost the same and they will never have a significant difference. 

Then I checked crypto-to-fiat pairs and I found they have regular divergence and convergence. It means we can apply a pairs trading strategy for these pairs.

**Example:**

Let's take USDT/UAH pair and assume that in some point of time they have following input data:

- Binance: Rate - 39.5 UAH per USDT, Balances - 40000 UAH, 1000 USDT
- WhiteBIT: Rate - 40 UAH per USDT, Balances - 40000 UAH, 1000 USDT

We have a ratio 0.9875 and the difference between rates 1.25%. Then we can open the following trades:

- Binance: buy 1000 USDT for 39500 UAH, residual balances - 2000 USDT, 500 UAH
- WhiteBIT: sell 1000 USDT for 40000 UAH, residual balances - 0 USDT, 80000 UAH

Next assumption would be that at some  point of time rates on Binance and WhiteBIT converges and becomes 39.75 UAH per USDT. Then we can make the following close trades:

- Binance: sell 1000 USDT for 39750 UAH, residual balances - 1000 USDT, 40250 UAH
- WhiteBIT: buy 1000 USDT for 39750 UAH, residual balance - 1000 USDT, 40250 UAH

As a result: 

- Total balance before:  2000 USDT, 80000 UAH
- Total balance after: 2000 USDT, 80500 UAH
- Difference: +500 UAH ~ 12.5$

Same profit can be received in all other possible cases:

- Rate on both exchanges grows up
- Rate on both exchanges falls down

It happens because our sell trade compensates buy trade and vice versa, so it shows that we don't rely on market movement 

Of course, in the example, we will consider the ideal variant of the price discrepancy without taking into account fees, but even with real trading, the strategy can bring profit.

# Implementation
As a base of the logic of my application, I took the open source project "link". This project is built on two principles:

- **Cointegration (add link to cointegration definition from wiki)** defines the relationship between pairs over time. But since my application considers identical pairs on different exchanges, this dependence is always present.
- **Z-score (add link to z-score definition in wiki)** is a formula that is designed to determine a deviation from the standard ratio of rates at the given moment.

We analyze the market on the Binance and WhiteBIT exchanges every minute and calculate the current z-score and, based on it, we decide to open a trade.  

For example, if the exchange rate on Binance is higher than on WhiteBIT, then we sell USDT on Binance and buy on Whitebit the same amount.

In the case when we have an open trade, we are looking for opportunities to close the trade.  To determine when it will be profitable to close a trade, we look for a z-score with the opposite sign from what it was at the time the trade was opened.  If at the time of opening the z-score was 1.8, then we need a z-score of -1.8 to close.

# Coding
**Technologies:**

- .NET 6
- .NET Background Worker
- Microsoft SQL Server 
- Entity Framework Code First approach
- Clean Architecture 
- Built-in .NET Dependency Injection

**External services:**

- Binance API to get orderbook (buy and sell orders), calculate a current exchange rate and make buy or sell trades using a market price
- WhiteBIT API to get orderbook (buy and sell orders), calculate a current exchange rate and make buy or sell trades using a market price
- Telegram API to send notifications about current status of pairs trading process (e.g. current market rates, notify about new trades)

**Developed services:**

- PairsService.cs: get a list of pairs and trading information from configuration
- PairsTradingCalculationService.cs: calculate z-score constants, average ratio and standard deviation
- PairsTradingService.cs: analyze market and open trades
- ProfitCalculator.cs: calculate a profit of trade
- BinanceService.cs: implementation of work with Binance API
- WhitebitService.cs: implementation of work with WhiteBIT API
- TelegramNotificationService.cs: implementation of work with Telegram API

**Background service workflow:**

1. Get a list of pairs and trading information from configuration (appsettings.json), example:

  "TradePairs": [

    {

      "CryptoCurrency": 0, //USDT

      "Currency": 3, //UAH

      "CryptoAmount": "1000", 

      "ActionDeviation": "1.8"

    }

]

2. For every pair:
3. Calculate and add to cache z-score constants (standard deviation and average ratio) using PairsTradingCalculationService.cs service
4. Get current rates using Binance and WhiteBIT APIs
5. Calculate Z-score based on constants and current market rate
6. Send a notification to Telegram about current market status 
7. In case if we don’t have open trades and current market situation allows us to open a new trade - we open this trade, otherwise skip and wait 1 minute
8. In case if we have some open trade and current market allows us to close the trade - close this trade, otherwise skip and wait 1 minute

**Possible improvements:**

- Refactor code to easily add more trade exchanges
- Refactor code to make it more readable, e.g. PairsTradingService.Process() method contains too many lines, a purpose of this was to quickly implement a main business logic and easily debug it and find errors
- Cover code with unit tests
- Add a web UI to easily control a tool workflow
