using AppCore.Constants;
using AppCore.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using TradingWorker;
using TradingWorker.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                x => x.MigrationsAssembly("Infrastructure")));

        var binanceCredentials = configuration.GetRequiredSection("Binance");
        services.AddSingleton<IBinanceAuthenticator>(serviceProvider =>
            new BinanceAuthenticator(binanceCredentials["ApiSecret"], binanceCredentials["ApiKey"]));

        var whitebitCredentials = configuration.GetRequiredSection("WhiteBIT");
        services.AddSingleton<IWhitebitAuthenticator>(serviceProvider =>
            new WhitebitAuthenticator(whitebitCredentials["ApiSecret"], whitebitCredentials["ApiKey"]));

        services.AddHttpClient(TradeExchangeType.Binance.ToString(), c =>
        {
            c.BaseAddress = new Uri("https://api.binance.com");
        });

        services.AddHttpClient(TradeExchangeType.WhiteBIT.ToString(), c =>
        {
            c.BaseAddress = new Uri("https://whitebit.com");
        });

        services.AddHttpClient("Telegram", c =>
        {
            c.BaseAddress = new Uri("https://api.telegram.org");
        });

        services.AddScoped<IPairsService, PairsService>();
        services.AddScoped<IBinanceService, BinanceService>();
        services.AddScoped<IWhitebitService, WhitebitService>();
        services.AddScoped<IPairsTradingCalculationService, PairsTradingCalculationService>();
        services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();
        services.AddScoped<IProfitCalculator, ProfitCalculator>();
        services.AddScoped<PairsTradingService>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();