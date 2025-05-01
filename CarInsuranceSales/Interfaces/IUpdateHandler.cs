using CarInsuranceSales.Handlers;
using Telegram.Bot.Types;

namespace CarInsuranceSales.Interfaces
{
    public interface IUpdateHandler
    {
        Task HandleUpdateAsync(Update update, ChatSessionData session);
    }
}
