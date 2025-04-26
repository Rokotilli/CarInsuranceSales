using CarInsuranceSales.Interfaces;
using Mindee.Product.Generated;
using Mindee.Product.InternationalId;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CarInsuranceSales.Handlers
{
    public class BaseBotHandler
    {
        protected async Task SendAiMessageWithTypingAsync(long chatId, string messagePrompt, TelegramBotClient botClient, IOpenRouterAPIService openRouterAPIService, InlineKeyboardMarkup inlineKeyboardMarkup)
        {
            await RunWithTypingAsync(botClient, chatId, async () =>
            {
                var aiResponse = await openRouterAPIService.GetMessageAsync(messagePrompt);
                await botClient.SendMessage(chatId, aiResponse.Choices[0].Message.Content, replyMarkup: inlineKeyboardMarkup);
            });
        }

        protected async Task RunWithTypingAsync(TelegramBotClient _botClient, long chatId, Func<Task> action)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var typingTask = Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await _botClient.SendChatAction(chatId, ChatAction.Typing);
                    await Task.Delay(4000, cancellationTokenSource.Token);
                }
            });

            try
            {
                await action();
            }
            finally
            {
                cancellationTokenSource.Cancel();
            }
        }

        protected InlineKeyboardMarkup InitializeInlineKeyboard(string yesData, string noData) =>
        new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Yes ✅", yesData),
            InlineKeyboardButton.WithCallbackData("No ❌", noData)
        });

        protected void InitializeDictionariesForChat(Dictionary<long, ChatSessionData> _sessions, long id)
        {
            if (!_sessions.ContainsKey(id))
                _sessions[id] = new ChatSessionData();
            else
                _sessions[id].Reset();
        }
    }

    public class ChatSessionData
    {
        public int PhotosProcessedCount { get; set; } = 0;
        public int ReceivedPhotosCount { get; set; } = 0;

        public bool IsInternationalDocumentProcessed { get; set; } = false;
        public bool IsGeneratedDocumentProcessed { get; set; } = false;

        public bool WeProcessedMessageSent { get; set; } = false;
        public bool HaveToSendMessage { get; set; } = true;

        public InternationalIdV2Document? InternationalDocument { get; set; }
        public GeneratedV1Document? GeneratedDocument { get; set; }

        public void Reset()
        {
            PhotosProcessedCount = 0;
            ReceivedPhotosCount = 0;
            IsInternationalDocumentProcessed = false;
            IsGeneratedDocumentProcessed = false;
            WeProcessedMessageSent = false;
            HaveToSendMessage = true;
            InternationalDocument = null;
            GeneratedDocument = null;
        }
    }
}
