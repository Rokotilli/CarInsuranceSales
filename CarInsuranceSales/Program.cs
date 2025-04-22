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

        services.AddHttpClient(config.OpenRouterAPI.Name, client =>
        {
            client.BaseAddress = new Uri(config.OpenRouterAPI.BaseAdress);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.OpenRouterAPI.APIKey}");
        });

        services.AddMindeeClient();

        services.AddSingleton(new TelegramBotClient(config.BotToken));

        services.AddScoped<IMindeeAPIService, MindeeAPIService>();
        services.AddScoped<ITelegramService, TelegramService>();
        services.AddScoped<IOpenRouterAPIService, OpenRouterAPIService>();

        services.AddScoped<BotHandler>();
    })
    .Build();

host.Services.GetRequiredService<BotHandler>();

Log.Logger.Information("Starting the bot...");

await host.RunAsync();