using CarInsuranceSales.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CarInsuranceSales.Handlers
{
    public class UpdateHandler : BaseBotHandler, IUpdateHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly IOpenRouterAPIService _openRouterAPIService;
        private readonly ILogger<IUpdateHandler> _logger;
        private readonly Config _config;

        public UpdateHandler(TelegramBotClient botClient, IOpenRouterAPIService openRouterAPIService, ILogger<IUpdateHandler> logger, IOptions<Config> options)
        {
            _botClient = botClient;
            _openRouterAPIService = openRouterAPIService;
            _logger = logger;
            _config = options.Value;
        }

        public async Task HandleUpdateAsync(Update update, ChatSessionData session)
        {
            var chatId = update.CallbackQuery.Message.Chat.Id;
            var messageId = update.CallbackQuery.Message.MessageId;

            switch (update.CallbackQuery.Data)
            {
                case "passportSubmitted":
                    session.PhotosProcessedCount = 1;
                    session.IsInternationalDocumentProcessed = false;
                    session.HaveToSendMessage = true;
                    session.WrongMessageReceivedMessageSent = false;

                    await SendAiMessageWithTypingAsync(chatId, _config.Messages.PassportSubmittedMessage, _botClient, _openRouterAPIService, null);

                    _logger.LogInformation($"Sent PassportSubmittedMessage message to chat: {chatId}");

                    break;
                case "passportRejected":
                    session.InternationalDocument = null;
                    session.IsInternationalDocumentProcessed = false;
                    session.HaveToSendMessage = true;
                    session.WrongMessageReceivedMessageSent = false;

                    await SendAiMessageWithTypingAsync(chatId, _config.Messages.DataRejectedMessage, _botClient, _openRouterAPIService, null);

                    _logger.LogInformation($"Sent DataRejectedMessage message to chat: {chatId}");

                    break;
                case "technicalPassportSubmitted":
                    session.PhotosProcessedCount = 2;
                    session.IsGeneratedDocumentProcessed = false;
                    session.HaveToSendMessage = true;
                    session.WrongMessageReceivedMessageSent = false;

                    await SendAiMessageWithTypingAsync(chatId, _config.Messages.DataSubmittedMessage, _botClient, _openRouterAPIService, InitializeInlineKeyboard("agreed", "disagreed"));

                    _logger.LogInformation($"Sent DataSubmittedMessage message to chat: {chatId}");

                    break;
                case "technicalPassportRejected":
                    session.IsGeneratedDocumentProcessed = false;
                    session.GeneratedDocument = null;
                    session.HaveToSendMessage = true;
                    session.WrongMessageReceivedMessageSent = false;

                    await SendAiMessageWithTypingAsync(chatId, _config.Messages.DataRejectedMessage, _botClient, _openRouterAPIService, null);

                    _logger.LogInformation($"Sent DataRejectedMessage message to chat: {chatId}");

                    break;
                case "agreed":
                    await RunWithTypingAsync(_botClient, chatId, async () =>
                    {
                        var aiResponse = await _openRouterAPIService.GetInsurancePolicyAsync(
                            session.InternationalDocument,
                            session.GeneratedDocument);

                        await _botClient.SendMessage(chatId, aiResponse.Choices[0].Message.Content);
                    });                    

                    _logger.LogInformation($"Sent AgreedMessage message to chat: {chatId}");

                    break;
                case "disagreed":
                    await SendAiMessageWithTypingAsync(chatId, _config.Messages.CostDisagreedMessage, _botClient, _openRouterAPIService, InitializeInlineKeyboard("agreed", "disagreed"));

                    _logger.LogInformation($"Sent DisagreedMessage message to chat: {chatId}");

                    break;
            }

            await _botClient.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null);
        }
    }
}
