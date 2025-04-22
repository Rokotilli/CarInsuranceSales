using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using CarInsuranceSales.Interfaces;
using Telegram.Bot.Types.ReplyMarkups;
using Mindee.Product.InternationalId;
using Mindee.Product.Generated;

namespace CarInsuranceSales
{
    public class BotHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly Config _config;
        private readonly ILogger<BotHandler> _logger;
        private readonly IMindeeAPIService _mindeeAPIService;
        private readonly IOpenRouterAPIService _openRouterAPIService;

        private Dictionary<long, InternationalIdV2Document> savedUserInternationalDocumentInChat = new();
        private Dictionary<long, GeneratedV1Document> savedUserGeneratedDocumentInChat = new();
        private Dictionary<long, bool> userInternationalDocumentProcessedInChat = new();
        private Dictionary<long, bool> userGeneratedDocumentProcessedInChat = new();
        private Dictionary<long, int> photosProcessedCountInChat = new();
        private Dictionary<long, int> receivedPhotosInChat = new();
        private Dictionary<long, bool> weProcessedYourPhotosMessageSentInChat = new();   
        private Dictionary<long, bool> haveToSendMessageInChat = new();

        public BotHandler(
            TelegramBotClient botClient,
            IOptions<Config> options,
            ILogger<BotHandler> logger,
            IMindeeAPIService mindeeAPIService,
            IOpenRouterAPIService openRouterAPIService)
        {
            _botClient = botClient;
            _config = options.Value;
            _logger = logger;
            _mindeeAPIService = mindeeAPIService;
            _openRouterAPIService = openRouterAPIService;

            _botClient.OnMessage += OnMessageReceivedAsync;
            _botClient.OnUpdate += OnUpdateReceivedAsync;            
        }

        private async Task OnMessageReceivedAsync(Message msg, UpdateType type)
        {
            _logger.LogInformation($"Received message: {msg.Text} from chat: {msg.Chat.Id}");

            if (msg.Text == "/start")
            {
                await RunWithTypingAsync(msg.Chat.Id, async () =>
                {
                    await _botClient.SendMessage(msg.Chat.Id, _config.Messages.StartMessage);
                });

                InitializeDictionariesForChat(msg);

                _logger.LogInformation($"Sent /start response to chat: {msg.Chat.Id}");

                return;
            }

            if (msg.Photo != null && (userInternationalDocumentProcessedInChat[msg.Chat.Id] || userGeneratedDocumentProcessedInChat[msg.Chat.Id]))
            {
                if (haveToSendMessageInChat[msg.Chat.Id])
                {
                    await RunWithTypingAsync(msg.Chat.Id, async () =>
                    {
                        var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.WeProcessedYourPhotoMessage);
                        await _botClient.SendMessage(msg.Chat.Id, aiResponse.Choices[0].Message.Content);
                    });

                    haveToSendMessageInChat[msg.Chat.Id] = false;

                    _logger.LogInformation($"Sent WeProcessedYourPhotoMessage message to chat: {msg.Chat.Id}");
                }

                return;
            }

            if (msg.Photo != null && msg.MediaGroupId != null)
            {
                if (haveToSendMessageInChat[msg.Chat.Id])
                {
                    await RunWithTypingAsync(msg.Chat.Id, async () =>
                    {
                        if (photosProcessedCountInChat[msg.Chat.Id] == 0)
                        {
                            var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.PassportShouldBeSentInOneMessage);

                            await _botClient.SendMessage(msg.Chat.Id, aiResponse.Choices[0].Message.Content);

                            _logger.LogInformation($"Sent PassportShouldBeSentInOneMessage message to chat: {msg.Chat.Id}");
                        }

                        if (photosProcessedCountInChat[msg.Chat.Id] == 1)
                        {
                            var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.TechnicalPassportShouldBeSentInOneMessage);

                            await _botClient.SendMessage(msg.Chat.Id, aiResponse.Choices[0].Message.Content);

                            _logger.LogInformation($"Sent TechnicalPassportShouldBeSentInOneMessage message to chat: {msg.Chat.Id}");
                        }
                    });

                    haveToSendMessageInChat[msg.Chat.Id] = false;
                }

                return;
            }

            if (msg.Photo != null && photosProcessedCountInChat[msg.Chat.Id] == 2)
            {
                if (!weProcessedYourPhotosMessageSentInChat[msg.Chat.Id] && haveToSendMessageInChat[msg.Chat.Id])
                {
                    await RunWithTypingAsync(msg.Chat.Id, async () =>
                    {
                        var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.WeSavedYourDataMessage);
                        await _botClient.SendMessage(msg.Chat.Id, aiResponse.Choices[0].Message.Content);
                    });

                    weProcessedYourPhotosMessageSentInChat[msg.Chat.Id] = true;

                    haveToSendMessageInChat[msg.Chat.Id] = false;

                    _logger.LogInformation($"Sent WeSavedYourDataMessage message to chat: {msg.Chat.Id}");
                }

                return;
            }

            if (msg.Photo != null)
            {
                _logger.LogInformation($"Received photo from chat: {msg.Chat.Id}");

                await RunWithTypingAsync(msg.Chat.Id, async () =>
                {

                    if (photosProcessedCountInChat[msg.Chat.Id] == 0)
                    {
                        var result = await _mindeeAPIService.ProcessInternationalIdAsync(msg.Photo.Last());

                        if (result.GivenNames.Count() == 0)
                        {
                            var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.IncorrectPhotoMessagge);

                            await _botClient.SendMessage(msg.Chat.Id, aiResponse.Choices[0].Message.Content);

                            _logger.LogInformation($"Sent IncorrectPhotosMessage message to chat: {msg.Chat.Id}");
                        }

                        var message = $"Name: {result.GivenNames[0].Value} {result.Surnames[0].Value}\n" +
                                      $"Document Number: {result.DocumentNumber.Value}\n" +
                                      $"Expiry Date: {result.ExpiryDate.Value.ToString()}\n\n" +
                                      "This information is correct?";

                        await _botClient.SendMessage(msg.Chat.Id, message, replyMarkup: InitializeInlineKeyboard("passportSubmitted", "passportRejected"));

                        receivedPhotosInChat[msg.Chat.Id] = 1;

                        userInternationalDocumentProcessedInChat[msg.Chat.Id] = true;

                        savedUserInternationalDocumentInChat[msg.Chat.Id] = result;

                        haveToSendMessageInChat[msg.Chat.Id] = true;

                        _logger.LogInformation($"Processed passport photo from chat: {msg.Chat.Id}");
                    }
                    else if (photosProcessedCountInChat[msg.Chat.Id] == 1)
                    {
                        var result = await _mindeeAPIService.ProcessVehicleIdentificationDocumentAsync(msg.Photo.Last());

                        if (result.Fields["vin"].Last().Values.Last().ValueKind == System.Text.Json.JsonValueKind.Null)
                        {
                            var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.IncorrectPhotoMessagge);

                            await _botClient.SendMessage(msg.Chat.Id, aiResponse.Choices[0].Message.Content);

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

                        savedUserGeneratedDocumentInChat[msg.Chat.Id] = result;

                        haveToSendMessageInChat[msg.Chat.Id] = true;

                        _logger.LogInformation($"Processed technical passport photo from chat: {msg.Chat.Id}");
                    }
                });
            }
        }

        private async Task OnUpdateReceivedAsync(Update update)
        {
            await RunWithTypingAsync(update.CallbackQuery.Message.Chat.Id, async () =>
            {
                if (update.CallbackQuery.Data == "passportSubmitted")
                {
                    photosProcessedCountInChat[update.CallbackQuery.Message.Chat.Id] = 1;

                    userInternationalDocumentProcessedInChat[update.CallbackQuery.Message.Chat.Id] = false;

                    haveToSendMessageInChat[update.CallbackQuery.Message.Chat.Id] = true;

                    var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.PassportSubmittedMessage);

                    await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, aiResponse.Choices[0].Message.Content);

                    _logger.LogInformation($"Sent PassportSubmittedMessage message to chat: {update.CallbackQuery.Message.Chat.Id}");
                }

                else if (update.CallbackQuery.Data == "passportRejected")
                {
                    savedUserInternationalDocumentInChat[update.CallbackQuery.Message.Chat.Id] = null;

                    userInternationalDocumentProcessedInChat[update.CallbackQuery.Message.Chat.Id] = false;

                    haveToSendMessageInChat[update.CallbackQuery.Message.Chat.Id] = true;

                    var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.DataRejectedMessage);

                    await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, aiResponse.Choices[0].Message.Content);

                    _logger.LogInformation($"Sent DataRejectedMessage message to chat: {update.CallbackQuery.Message.Chat.Id}");
                }

                if (update.CallbackQuery.Data == "technicalPassportSubmitted")
                {
                    photosProcessedCountInChat[update.CallbackQuery.Message.Chat.Id] = 2;

                    userGeneratedDocumentProcessedInChat[update.CallbackQuery.Message.Chat.Id] = false;

                    haveToSendMessageInChat[update.CallbackQuery.Message.Chat.Id] = true;

                    var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.DataSubmittedMessage);

                    await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, aiResponse.Choices[0].Message.Content, replyMarkup: InitializeInlineKeyboard("agreed", "disagreed"));

                    _logger.LogInformation($"Sent DataSubmittedMessage message to chat: {update.CallbackQuery.Message.Chat.Id}");
                }
                else if (update.CallbackQuery.Data == "technicalPassportRejected")
                {
                    userGeneratedDocumentProcessedInChat[update.CallbackQuery.Message.Chat.Id] = false;

                    savedUserGeneratedDocumentInChat[update.CallbackQuery.Message.Chat.Id] = null;

                    haveToSendMessageInChat[update.CallbackQuery.Message.Chat.Id] = true;

                    var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.DataRejectedMessage);

                    await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, aiResponse.Choices[0].Message.Content);

                    _logger.LogInformation($"Sent DataRejectedMessage message to chat: {update.CallbackQuery.Message.Chat.Id}");
                }

                else if (update.CallbackQuery.Data == "agreed")
                {
                    var aiResponse = await _openRouterAPIService.GetInsurancePolicyAsync(
                        savedUserInternationalDocumentInChat[update.CallbackQuery.Message.Chat.Id],
                        savedUserGeneratedDocumentInChat[update.CallbackQuery.Message.Chat.Id]);

                    await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, aiResponse.Choices[0].Message.Content);

                    _logger.LogInformation($"Sent AgreedMessage message to chat: {update.CallbackQuery.Message.Chat.Id}");
                }
                else if (update.CallbackQuery.Data == "disagreed")
                {
                    var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.CostDisagreedMessage);

                    await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, aiResponse.Choices[0].Message.Content, replyMarkup: InitializeInlineKeyboard("agreed", "disagreed"));

                    _logger.LogInformation($"Sent DisagreedMessage message to chat: {update.CallbackQuery.Message.Chat.Id}");
                }

                await _botClient.EditMessageReplyMarkup(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, replyMarkup: null);
            });
        }

        private async Task RunWithTypingAsync(long chatId, Func<Task> action)
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

            if (!haveToSendMessageInChat.ContainsKey(msg.Chat.Id))
            {
                haveToSendMessageInChat.Add(msg.Chat.Id, true);
            }
            else
            {
                haveToSendMessageInChat[msg.Chat.Id] = true;
            }

            if (!savedUserInternationalDocumentInChat.ContainsKey(msg.Chat.Id))
            {
                savedUserInternationalDocumentInChat.Add(msg.Chat.Id, null);
            }
            else
            {
                savedUserInternationalDocumentInChat[msg.Chat.Id] = null;
            }

            if (!savedUserGeneratedDocumentInChat.ContainsKey(msg.Chat.Id))
            {
                savedUserGeneratedDocumentInChat.Add(msg.Chat.Id, null);
            }
            else
            {
                savedUserGeneratedDocumentInChat[msg.Chat.Id] = null;
            }
        }
    }
}
