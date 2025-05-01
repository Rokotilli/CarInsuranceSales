using CarInsuranceSales.Handlers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CarInsuranceSales.Interfaces
{
    public interface IMessageHandler
    {
        Task HandleMessageAsync(Message msg, UpdateType type, Dictionary<long, ChatSessionData> sessions);
    }
}
