using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using CarInsuranceSales.Interfaces;
using Telegram.Bot.Types.ReplyMarkups;

namespace CarInsuranceSales
{
    public class BotHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly Config _config;
        private readonly ILogger<BotHandler> _logger;
        private readonly IMindeeAPIService _mindeeAPIService;

        private Dictionary<long, bool> userInternationalDocumentProcessedInChat = new();
        private Dictionary<long, bool> userGeneratedDocumentProcessedInChat = new();
        private Dictionary<long, int> photosProcessedCountInChat = new();
        private Dictionary<long, int> receivedPhotosInChat = new();
        private Dictionary<long, bool> weProcessedYourPhotosMessageSentInChat = new();        

        public BotHandler(TelegramBotClient botClient, IOptions<Config> options, ILogger<BotHandler> logger, IMindeeAPIService mindeeAPIService)
        {
            _botClient = botClient;
            _config = options.Value;
            _logger = logger;
            _mindeeAPIService = mindeeAPIService;

            _botClient.OnMessage += OnMessageReceivedAsync;
            _botClient.OnUpdate += OnUpdateReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(Message msg, UpdateType type)
        {
            _logger.LogInformation($"Received message: {msg.Text} from chat: {msg.Chat.Id}");

            if (msg.Text == "/start")
            {
                await _botClient.SendMessage(msg.Chat.Id, _config.Messages.StartMessage);

                InitializeDictionariesForChat(msg);

                _logger.LogInformation($"Sent /start response to chat: {msg.Chat.Id}");

                return;
            }

            if (msg.Photo != null && (userInternationalDocumentProcessedInChat[msg.Chat.Id] || userGeneratedDocumentProcessedInChat[msg.Chat.Id]))
            {
                await _botClient.SendMessage(msg.Chat.Id, _config.Messages.WeProcessedYourPhotoMessage);

                _logger.LogInformation($"Sent WeProcessedYourPhotoMessage message to chat: {msg.Chat.Id}");

                return;
            }

            if (msg.Photo != null && msg.MediaGroupId != null)
            {
                if (photosProcessedCountInChat[msg.Chat.Id] == 0)
                {
                    await _botClient.SendMessage(msg.Chat.Id, _config.Messages.PassportShouldBeSentInOneMessage);

                    _logger.LogInformation($"Sent PassportShouldBeSentInOneMessage message to chat: {msg.Chat.Id}");                    
                }

                if (photosProcessedCountInChat[msg.Chat.Id] == 1)
                {
                    await _botClient.SendMessage(msg.Chat.Id, _config.Messages.TechnicalPassportShouldBeSentInOneMessage);

                    _logger.LogInformation($"Sent TechnicalPassportShouldBeSentInOneMessage message to chat: {msg.Chat.Id}");
                }

                return;
            }            

            if (msg.Photo != null && photosProcessedCountInChat[msg.Chat.Id] == 2)
            {
                if (!weProcessedYourPhotosMessageSentInChat[msg.Chat.Id])
                {
                    await _botClient.SendMessage(msg.Chat.Id, _config.Messages.WeSavedYourDataMessage);

                    weProcessedYourPhotosMessageSentInChat[msg.Chat.Id] = true;

                    _logger.LogInformation($"Sent WeSavedYourDataMessage message to chat: {msg.Chat.Id}");
                }

                return;
            }            

            if (msg.Photo != null)
            {
                _logger.LogInformation($"Received photo from chat: {msg.Chat.Id}");

                if (photosProcessedCountInChat[msg.Chat.Id] == 0)
                {
                    var result = await _mindeeAPIService.ProcessInternationalIdAsync(msg.Photo.Last());

                    if (result.GivenNames.Count() == 0)
                    {
                        await _botClient.SendMessage(msg.Chat.Id, _config.Messages.IncorrectPhotoMessagge);

                        _logger.LogInformation($"Sent IncorrectPhotosMessage message to chat: {msg.Chat.Id}");
                    }

                    var message = $"Name: {result.GivenNames[0].Value} {result.Surnames[0].Value}\n" +
                                  $"Document Number: {result.DocumentNumber.Value}\n" +
                                  $"Expiry Date: {result.ExpiryDate.Value.ToString()}\n" +
                                  "This information is correct?";

                    await _botClient.SendMessage(msg.Chat.Id, message, replyMarkup: InitializeInlineKeyboard("passportSubmitted", "passportRejected"));                    

                    receivedPhotosInChat[msg.Chat.Id] = 1;

                    userInternationalDocumentProcessedInChat[msg.Chat.Id] = true;

                    _logger.LogInformation($"Processed passport photo from chat: {msg.Chat.Id} with MediaGroupId: {msg.MediaGroupId}");
                }
                else if (photosProcessedCountInChat[msg.Chat.Id] == 1)
                {
                    var result = await _mindeeAPIService.ProcessVehicleIdentificationDocumentAsync(msg.Photo.Last());

                    if (result.Fields["vin"].Last().Values.Last().ValueKind == System.Text.Json.JsonValueKind.Null)
                    {
                        await _botClient.SendMessage(msg.Chat.Id, _config.Messages.IncorrectPhotoMessagge);

                        _logger.LogInformation($"Sent IncorrectPhotosMessage message to chat: {msg.Chat.Id}");
                    }

                    var message = $"VIN: {result.Fields["vin"].Last().Values.Last().GetRawText()}\n" +
                                  $"License plate: {result.Fields["license_plate"].Last().Values.Last().GetRawText()}\n" +
                                  $"Manufacturer: {result.Fields["manufacturer"].Last().Values.Last().GetRawText()}\n" +
                                  $"Model: {result.Fields["model"].Last().Values.Last().GetRawText()}\n" +
                                  $"Manufacturing date: {result.Fields["manufacturing_date"].Last().Values.Last().GetRawText()}\n\n" +
                                  "This information is correct?";

                    await _botClient.SendMessage(msg.Chat.Id, message, replyMarkup: InitializeInlineKeyboard("technicalPassportSubmitted", "technicalPassportRejected"));

                    receivedPhotosInChat[msg.Chat.Id] = 2;

                    userGeneratedDocumentProcessedInChat[msg.Chat.Id] = true;      

                    _logger.LogInformation($"Processed technical passport photo from chat: {msg.Chat.Id} with MediaGroupId: {msg.MediaGroupId}");
                }
            }
        }        

        private async Task OnUpdateReceivedAsync(Update update)
        {
            if (update.CallbackQuery.Data == "passportSubmitted")
            {
                photosProcessedCountInChat[update.CallbackQuery.Message.Chat.Id] = 1;

                userInternationalDocumentProcessedInChat[update.CallbackQuery.Message.Chat.Id] = false;

                await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, _config.Messages.PassportSubmittedMessage);

                _logger.LogInformation($"Sent PassportSubmittedMessage message to chat: {update.CallbackQuery.Message.Chat.Id}");
            }

            else if (update.CallbackQuery.Data == "passportRejected")
            {
                userInternationalDocumentProcessedInChat[update.CallbackQuery.Message.Chat.Id] = false;

                await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, _config.Messages.DataRejectedMessage);

                _logger.LogInformation($"Sent DataRejectedMessage message to chat: {update.CallbackQuery.Message.Chat.Id}");
            }

            if (update.CallbackQuery.Data == "technicalPassportSubmitted")
            {
                photosProcessedCountInChat[update.CallbackQuery.Message.Chat.Id] = 2;

                userGeneratedDocumentProcessedInChat[update.CallbackQuery.Message.Chat.Id] = false;

                await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, _config.Messages.DataSubmittedMessage, replyMarkup: InitializeInlineKeyboard("agreed", "disagreed"));

                _logger.LogInformation($"Sent DataSubmittedMessage message to chat: {update.CallbackQuery.Message.Chat.Id}");
            }
            else if (update.CallbackQuery.Data == "technicalPassportRejected")
            {
                userGeneratedDocumentProcessedInChat[update.CallbackQuery.Message.Chat.Id] = false;

                await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, _config.Messages.DataRejectedMessage);

                _logger.LogInformation($"Sent DataRejectedMessage message to chat: {update.CallbackQuery.Message.Chat.Id}");
            }

            else if (update.CallbackQuery.Data == "agreed")
            {
                await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, "Here is insurance, sign pls.");

                _logger.LogInformation($"Sent AgreedMessage message to chat: {update.CallbackQuery.Message.Chat.Id}");
            }
            else if (update.CallbackQuery.Data == "disagreed")
            {
                await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, _config.Messages.CostDisagreedMessage, replyMarkup: InitializeInlineKeyboard("agreed", "disagreed"));

                _logger.LogInformation($"Sent DisagreedMessage message to chat: {update.CallbackQuery.Message.Chat.Id}");
            }

            await _botClient.EditMessageReplyMarkup(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, replyMarkup: null);
        }

        private InlineKeyboardMarkup InitializeInlineKeyboard(string yesData, string noData) =>
        new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Yes ✅", yesData),
            InlineKeyboardButton.WithCallbackData("No ❌", noData)
        });

        private void InitializeDictionariesForChat(Message msg)
        {
            if (!receivedPhotosInChat.ContainsKey(msg.Chat.Id))
            {
                receivedPhotosInChat.Add(msg.Chat.Id, 0);
            }
            else
            {
                receivedPhotosInChat[msg.Chat.Id] = 0;
            }

            if (!photosProcessedCountInChat.ContainsKey(msg.Chat.Id))
            {
                photosProcessedCountInChat.Add(msg.Chat.Id, 0);
            }
            else
            {
                photosProcessedCountInChat[msg.Chat.Id] = 0;
            }

            if (!userInternationalDocumentProcessedInChat.ContainsKey(msg.Chat.Id))
            {
                userInternationalDocumentProcessedInChat.Add(msg.Chat.Id, false);
            }
            else
            {
                userInternationalDocumentProcessedInChat[msg.Chat.Id] = false;
            }

            if (!userGeneratedDocumentProcessedInChat.ContainsKey(msg.Chat.Id))
            {
                userGeneratedDocumentProcessedInChat.Add(msg.Chat.Id, false);
            }
            else
            {
                userGeneratedDocumentProcessedInChat[msg.Chat.Id] = false;
            }

            if (!weProcessedYourPhotosMessageSentInChat.ContainsKey(msg.Chat.Id))
            {
                weProcessedYourPhotosMessageSentInChat.Add(msg.Chat.Id, false);
            }
            else
            {
                weProcessedYourPhotosMessageSentInChat[msg.Chat.Id] = false;
            }
        }
    }
}
