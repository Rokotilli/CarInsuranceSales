using CarInsuranceSales.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CarInsuranceSales.Handlers
{
    public class MessageHandler : BaseBotHandler, IMessageHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly IOpenRouterAPIService _openRouterAPIService;
        private readonly IMindeeAPIService _mindeeAPIService;
        private readonly ILogger<IMessageHandler> _logger;
        private readonly Config _config;

        public MessageHandler(
            TelegramBotClient botClient,
            IOpenRouterAPIService openRouterAPIService,
            IMindeeAPIService mindeeAPIService,
            ILogger<IMessageHandler> logger,
            IOptions<Config> options)
        {
            _botClient = botClient;
            _openRouterAPIService = openRouterAPIService;
            _mindeeAPIService = mindeeAPIService;
            _logger = logger;
            _config = options.Value;
        }

        public async Task HandleMessageAsync(Message msg, UpdateType type, Dictionary<long, ChatSessionData> sessions)
        {
            _logger.LogInformation($"Received message: {msg.Text} from chat: {msg.Chat.Id}");

            if (msg.Text == "/start")
            {
                await _botClient.SendMessage(msg.Chat.Id, _config.Messages.StartMessage);

                InitializeDictionariesForChat(sessions, msg.Chat.Id);

                _logger.LogInformation($"Sent /start response to chat: {msg.Chat.Id}");

                return;
            }

            try
            {
                if (msg.Photo != null)
                {
                    if (sessions[msg.Chat.Id].IsInternationalDocumentProcessed || sessions[msg.Chat.Id].IsGeneratedDocumentProcessed)
                    {
                        if (sessions[msg.Chat.Id].HaveToSendMessage)
                        {
                            await SendAiMessageWithTypingAsync(msg.Chat.Id, _config.Messages.WeProcessedYourPhotoMessage, _botClient, _openRouterAPIService, null);

                            sessions[msg.Chat.Id].HaveToSendMessage = true;

                            _logger.LogInformation($"Sent WeProcessedYourPhotoMessage to chat: {msg.Chat.Id}");
                        }

                        return;
                    }

                    if (msg.MediaGroupId != null)
                    {
                        if (sessions[msg.Chat.Id].HaveToSendMessage)
                        {
                            if (sessions[msg.Chat.Id].PhotosProcessedCount == 0)
                            {
                                await SendAiMessageWithTypingAsync(msg.Chat.Id, _config.Messages.PassportShouldBeSentInOneMessage, _botClient, _openRouterAPIService, null);

                                _logger.LogInformation($"Sent PassportShouldBeSentInOneMessage message to chat: {msg.Chat.Id}");
                            }

                            if (sessions[msg.Chat.Id].PhotosProcessedCount == 1)
                            {
                                await SendAiMessageWithTypingAsync(msg.Chat.Id, _config.Messages.TechnicalPassportShouldBeSentInOneMessage, _botClient, _openRouterAPIService, null);

                                _logger.LogInformation($"Sent TechnicalPassportShouldBeSentInOneMessage message to chat: {msg.Chat.Id}");
                            }

                            sessions[msg.Chat.Id].HaveToSendMessage = false;
                        }

                        return;
                    }

                    if (sessions[msg.Chat.Id].PhotosProcessedCount == 2 && !sessions[msg.Chat.Id].WeSavedYourDataMessageSent && sessions[msg.Chat.Id].HaveToSendMessage)
                    {
                        await SendAiMessageWithTypingAsync(msg.Chat.Id, _config.Messages.WeSavedYourDataMessage, _botClient, _openRouterAPIService, null);

                        sessions[msg.Chat.Id].WeSavedYourDataMessageSent = true;
                        sessions[msg.Chat.Id].HaveToSendMessage = false;

                        _logger.LogInformation($"Sent WeSavedYourDataMessage to chat: {msg.Chat.Id}");

                        return;
                    }

                    _logger.LogInformation($"Received photo from chat: {msg.Chat.Id}");

                    await RunWithTypingAsync(_botClient, msg.Chat.Id, async () =>
                    {
                        if (sessions[msg.Chat.Id].PhotosProcessedCount == 0)
                        {
                            await ProcessPassportPhotoAsync(msg.Chat.Id, msg, sessions[msg.Chat.Id]);
                        }
                        else if (sessions[msg.Chat.Id].PhotosProcessedCount == 1)
                        {
                            await ProcessTechnicalPassportPhotoAsync(msg.Chat.Id, msg, sessions[msg.Chat.Id]);
                        }
                    });
                }
                else
                {                    
                    if (!sessions[msg.Chat.Id].WrongMessageReceivedMessageSent)
                    {
                        sessions[msg.Chat.Id].WrongMessageReceivedMessageSent = true;

                        await SendAiMessageWithTypingAsync(msg.Chat.Id, _config.Messages.WrongMessage, _botClient, _openRouterAPIService, null);

                        _logger.LogInformation($"Sent WrongMessage to chat: {msg.Chat.Id}");                        
                    }                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message: {msg.Text} from chat: {msg.Chat.Id} error: {ex.Message}");
                await SendAiMessageWithTypingAsync(msg.Chat.Id, _config.Messages.ErrorMessage, _botClient, _openRouterAPIService, null);
            }            
        }

        private async Task ProcessPassportPhotoAsync(long chatId, Message msg, ChatSessionData session)
        {
            var result = await _mindeeAPIService.ProcessInternationalIdAsync(msg.Photo.Last());

            if (!result.GivenNames.Any())
            {
                var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.IncorrectPhotoMessagge);

                await _botClient.SendMessage(msg.Chat.Id, aiResponse.Choices[0].Message.Content);

                _logger.LogInformation($"Sent IncorrectPhotosMessage message to chat: {chatId}");

                return;
            }

            var message = $"Full Name: {result.GivenNames[0].Value} {result.Surnames[0].Value}\n" +
                          $"Passport Number: {result.DocumentNumber.Value}\n" +
                          $"Expiry Date: {result.ExpiryDate.Value}\n" +
                          $"Issuing country: {result.CountryOfIssue.Value}\n" +
                          $"Personal number: {result.PersonalNumber.Value}\n" +
                          $"Sex: {result.Sex.Value}\n" +
                          $"Nationality: {result.Nationality.Value}\n" +
                          $"Date of birth: {result.BirthDate.Value}\n\n" +
                          "This information is correct?";

            await _botClient.SendMessage(chatId, message, replyMarkup: InitializeInlineKeyboard("passportSubmitted", "passportRejected"));

            session.ReceivedPhotosCount = 1;
            session.IsInternationalDocumentProcessed = true;
            session.InternationalDocument = result;
            session.HaveToSendMessage = true;
            session.WrongMessageReceivedMessageSent = false;

            _logger.LogInformation($"Processed passport photo from chat: {chatId}");
        }

        private async Task ProcessTechnicalPassportPhotoAsync(long chatId, Message msg, ChatSessionData session)
        {
            var result = await _mindeeAPIService.ProcessVehicleIdentificationDocumentAsync(msg.Photo.Last());

            if (result.Fields["vehicle_identification_number"].Last().Values.Last().ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                var aiResponse = await _openRouterAPIService.GetMessageAsync(_config.Messages.IncorrectPhotoMessagge);

                await _botClient.SendMessage(msg.Chat.Id, aiResponse.Choices[0].Message.Content);

                _logger.LogInformation($"Sent IncorrectPhotosMessage message to chat: {msg.Chat.Id}");

                return;
            }

            var message = $"VIN: {result.Fields["vehicle_identification_number"].Last().Values.Last().GetRawText()}\n" +
                          $"Manufacturer: {result.Fields["manufacturer"].Last().Values.Last().GetRawText()}\n" +
                          $"Model: {result.Fields["model"].Last().Values.Last().GetRawText()}\n" +
                          $"Color: {Regex.Unescape(result.Fields["color"].Last().Values.Last().GetRawText())}\n\n" +
                          "This information is correct?";

            await _botClient.SendMessage(msg.Chat.Id, message, replyMarkup: InitializeInlineKeyboard("technicalPassportSubmitted", "technicalPassportRejected"));

            session.ReceivedPhotosCount = 2;
            session.IsGeneratedDocumentProcessed = true;
            session.GeneratedDocument = result;
            session.HaveToSendMessage = true;
            session.WrongMessageReceivedMessageSent = false;

            _logger.LogInformation($"Processed technical passport photo from chat: {msg.Chat.Id}");
        }
    }
}
