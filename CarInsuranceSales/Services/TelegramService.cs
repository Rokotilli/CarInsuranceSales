using CarInsuranceSales.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CarInsuranceSales.Services
{
    public class TelegramService : ITelegramService
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<ITelegramService> _logger;

        public TelegramService(TelegramBotClient telegramBotClient, ILogger<ITelegramService> logger)
        {
            _botClient = telegramBotClient;
            _logger = logger;
        }

        public async Task<string> DownloadFileAndGetPath(PhotoSize photo)
        {
            try
            {  
                var fileId = photo.FileId;
                var file = await _botClient.GetFile(fileId);

                var localFilePath = Path.Combine("TempFiles", $"{file.FileId}.jpg");

                _logger.LogInformation($"Downloading file to {localFilePath}");

                Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));

                using (var saveImageStream = new FileStream(localFilePath, FileMode.Create))
                {
                    await _botClient.DownloadFile(file.FilePath, saveImageStream);
                }

                _logger.LogInformation($"File downloaded to {localFilePath}");

                return localFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from Telegram");
                throw ex;
            }            
        }
    }
}
