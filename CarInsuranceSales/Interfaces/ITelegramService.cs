using Telegram.Bot.Types;

namespace CarInsuranceSales.Interfaces
{
    public interface ITelegramService
    {
        Task<string> DownloadFileAndGetPath(PhotoSize msg);
    }
}
