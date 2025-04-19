using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using CarInsuranceSales.Interfaces;

namespace CarInsuranceSales
{
    public class BotHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly Config _config;
        private readonly ILogger<BotHandler> _logger;
        private readonly IMindeeAPIService _mindeeAPIService;
        private readonly ITelegramService _telegramService;

        private Dictionary<string, PhotoSize> photosInMediaGroup = new();

        public BotHandler(TelegramBotClient botClient, IOptions<Config> options, ILogger<BotHandler> logger, IMindeeAPIService mindeeAPIService, ITelegramService telegramService)
        {
            _botClient = botClient;
            _config = options.Value;
            _logger = logger;
            _mindeeAPIService = mindeeAPIService;
            _telegramService = telegramService;

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

            if (msg.Photo != null && msg.MediaGroupId == null)
            {
                await _botClient.SendMessage(msg.Chat.Id, _config.PhotosShouldBeInOneMessage);

                _logger.LogInformation($"Sent PhotosShouldBeInOneMessage message to chat: {msg.Chat.Id}");
            }

            if (msg.Photo != null && msg.MediaGroupId != null)
            {
                _logger.LogInformation($"Received photo from chat: {msg.Chat.Id}");                

                if (!photosInMediaGroup.ContainsKey(msg.MediaGroupId))
                {
                    photosInMediaGroup.Add(msg.MediaGroupId, msg.Photo.Last());

                    var photoPath = await _telegramService.DownloadFileAndGetPath(msg.Photo.Last());

                    var result = await _mindeeAPIService.ProcessInternationalIdAsync(photoPath);

                    if (result.GivenNames.Count() == 0)
                    {
                        await _botClient.SendMessage(msg.Chat.Id, _config.IncorrectPhotosMessage);

                        _logger.LogInformation($"Sent IncorrectPhotosMessage message to chat: {msg.Chat.Id}");
                    }

                    _logger.LogInformation($"Processed passport photo from chat: {msg.Chat.Id} with MediaGroupId: {msg.MediaGroupId}");
                }
                else
                {
                    var photoPath = await _telegramService.DownloadFileAndGetPath(msg.Photo.Last());

                    var result = await _mindeeAPIService.ProcessVehicleIdentificationDocumentAsync(photoPath);

                    if (result.Fields["vin"].Last().Values.Last().ValueKind == System.Text.Json.JsonValueKind.Null)
                    {
                        await _botClient.SendMessage(msg.Chat.Id, _config.IncorrectPhotosMessage);

                        _logger.LogInformation($"Sent IncorrectPhotosMessage message to chat: {msg.Chat.Id}");
                    }

                    _logger.LogInformation($"Processed technical passport photo from chat: {msg.Chat.Id} with MediaGroupId: {msg.MediaGroupId}");
                }
            }
        }
    }
}
