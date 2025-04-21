using CarInsuranceSales;
using CarInsuranceSales.Interfaces;
using CarInsuranceSales.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mindee.Extensions.DependencyInjection;
using Serilog;
using Telegram.Bot;

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((ctx, con) =>
    {
        con.AddJsonFile("appconfig.json", false, true).AddUserSecrets<Program>();
    })
    .UseSerilog()
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<Config>(ctx.Configuration);

        var config = ctx.Configuration.Get<Config>();

        services.AddMindeeClient();

        services.AddSingleton(new TelegramBotClient(config.BotToken));

        services.AddScoped<IMindeeAPIService, MindeeAPIService>();
        services.AddScoped<ITelegramService, TelegramService>();

        services.AddScoped<BotHandler>();
    })
    .Build();

host.Services.GetRequiredService<BotHandler>();

Log.Logger.Information("Starting the bot...");

await host.RunAsync();