using Telegram.Bot;
using CarInsuranceSales.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CarInsuranceSales.Handlers
{
    public class BotHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly IMessageHandler _messageHandler;
        private readonly IUpdateHandler _updateHandler;

        private Dictionary<long, ChatSessionData> _sessions = new();

        public BotHandler(
            TelegramBotClient botClient,
            IMessageHandler messageHandler,
            IUpdateHandler updateHandler)
        {
            _botClient = botClient;
            _messageHandler = messageHandler;
            _updateHandler = updateHandler;

            _botClient.OnMessage += OnMessageAsync;
            _botClient.OnUpdate += OnUpdateAsync;            
        }

        public async Task OnMessageAsync(Message msg, UpdateType type)
        {
            await _messageHandler.HandleMessageAsync(msg, type, _sessions);
        }

        public async Task OnUpdateAsync(Update upd)
        {
            await _updateHandler.HandleUpdateAsync(upd, _sessions[upd.CallbackQuery.Message.Chat.Id]);
        }
    }
}
