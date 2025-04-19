using CarInsuranceSales;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        services.AddSingleton(new TelegramBotClient(config.BotToken));

        services.AddScoped<BotHandler>();
    })
    .Build();

host.Services.GetRequiredService<BotHandler>();

Log.Logger.Information("Starting the bot...");

await host.RunAsync();