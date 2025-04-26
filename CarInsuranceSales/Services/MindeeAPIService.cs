using CarInsuranceSales.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mindee;
using Mindee.Http;
using Mindee.Input;
using Mindee.Product.Generated;
using Mindee.Product.InternationalId;
using Telegram.Bot.Types;

namespace CarInsuranceSales.Services
{
    public class MindeeAPIService : IMindeeAPIService
    {
        private readonly MindeeClient _mindeeClient;
        private readonly ITelegramService _telegramService;
        private readonly ILogger<IMindeeAPIService> _logger;
        private readonly Config _config;

        public MindeeAPIService(MindeeClient mindeeClient, ILogger<IMindeeAPIService> logger, ITelegramService telegramService, IOptions<Config> options)
        {
            _mindeeClient = mindeeClient;
            _logger = logger;
            _telegramService = telegramService;
            _config = options.Value;
        }

        public async Task<InternationalIdV2Document> ProcessInternationalIdAsync(PhotoSize photo)
        {
            var filePath = await _telegramService.DownloadFileAndGetPath(photo);

            _logger.LogInformation($"Processing International ID from file: {filePath}");

            var inputSource = new LocalInputSource(filePath);

            var response = await _mindeeClient.EnqueueAndParseAsync<InternationalIdV2>(inputSource);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _logger.LogInformation($"Processed International ID for: " + (response.Document.Inference.Prediction.GivenNames.Count() != 0 ? response.Document.Inference.Prediction.GivenNames[0].Value : "NotFound"));

            return response.Document.Inference.Prediction;
        }

        public async Task<GeneratedV1Document> ProcessVehicleIdentificationDocumentAsync(PhotoSize photo)
        {
            var filePath = await _telegramService.DownloadFileAndGetPath(photo);

            _logger.LogInformation($"Processing Vehicle Identification Document from file: {filePath}");

            var inputSource = new LocalInputSource(filePath);

            var endpoint = new CustomEndpoint(
                endpointName: _config.Mindee.EndpointName,
                accountName: _config.Mindee.AccountName,
                version: _config.Mindee.Version
            );

            var response = await _mindeeClient.EnqueueAndParseAsync<GeneratedV1>(inputSource, endpoint);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _logger.LogInformation($"Processed Vehicle Identification Document with VIN: {response.Document.Inference.Prediction.Fields["vehicle_identification_number"].Last().Values.Last().GetRawText()}");

            return response.Document.Inference.Prediction;
        }
    }
}
