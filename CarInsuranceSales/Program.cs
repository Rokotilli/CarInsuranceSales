using CarInsuranceSales;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((ctx, con) =>
    {
        con.AddJsonFile("appconfig.json", false, true).AddUserSecrets<Program>();
    })
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<Config>(ctx.Configuration);

        var config = ctx.Configuration.Get<Config>();

        services.AddSingleton(new TelegramBotClient(config.BotToken));
    })
    .Build();

await host.RunAsync();