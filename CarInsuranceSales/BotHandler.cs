using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CarInsuranceSales
{
    public class BotHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly Config _config;
        private readonly ILogger<BotHandler> _logger;

        public BotHandler(TelegramBotClient botClient, IOptions<Config> options, ILogger<BotHandler> logger)
        {
            _botClient = botClient;
            _config = options.Value;
            _logger = logger;

            _botClient.OnMessage += OnMessageReceivedAsync;            
        }

        private async Task OnMessageReceivedAsync(Message msg, UpdateType type)
        {
            _logger.LogInformation($"Received message: {msg.Text} from chat: {msg.Chat.Id}");

            if (msg.Text == "/start")
            {
                await _botClient.SendMessage(msg.Chat.Id, _config.StartMessage);

                _logger.LogInformation($"Sent /start response to chat: {msg.Chat.Id}");
            }
        }
    }
}
