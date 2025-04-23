namespace CarInsuranceSales.BackgroundWorkers
{
    public class KeepAliveWorker : BackgroundService
    {
        private readonly ILogger<KeepAliveWorker> _logger;

        public KeepAliveWorker(ILogger<KeepAliveWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            app.Urls.Add("http://0.0.0.0:1000");

            app.MapGet("/", () => "Bot is running!");

            _logger.LogInformation("Starting keep-alive web server on default port...");
            await app.RunAsync(stoppingToken);
        }
    }
}
