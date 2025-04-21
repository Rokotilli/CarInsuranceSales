using CarInsuranceSales.Interfaces;
using Microsoft.Extensions.Logging;
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

        public MindeeAPIService(MindeeClient mindeeClient, ILogger<IMindeeAPIService> logger, ITelegramService telegramService)
        {
            _mindeeClient = mindeeClient;
            _logger = logger;
            _telegramService = telegramService;
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

            //var mockInternationalIdV2Document = new InternationalIdV2Document
            //{
            //    GivenNames = new List<StringField>
            //    {
            //        new StringField("John", "John", 0.99, null, null),
            //    },
            //    Surnames = new List<StringField>
            //    {
            //        new StringField("Doe", "Doe", 0.99, null, null),
            //    },
            //    DocumentNumber = new StringField("A12345678", "A12345678", 0.99, null, null),
            //    ExpiryDate = new DateField("2030-01-01", 0.99, null, null, false)
            //};

            //_logger.LogInformation($"Processed International ID for: " + (mockInternationalIdV2Document.GivenNames.Count() != 0 ? mockInternationalIdV2Document.GivenNames[0].Value : "NotFound"));

            //return mockInternationalIdV2Document;
        }

        public async Task<GeneratedV1Document> ProcessVehicleIdentificationDocumentAsync(PhotoSize photo)
        {
            var filePath = await _telegramService.DownloadFileAndGetPath(photo);

            _logger.LogInformation($"Processing Vehicle Identification Document from file: {filePath}");

            var inputSource = new LocalInputSource(filePath);

            var endpoint = new CustomEndpoint(
                endpointName: "vehicle_identification_document",
                accountName: "Toteman",
                version: "1"
            );

            var response = await _mindeeClient.EnqueueAndParseAsync<GeneratedV1>(inputSource, endpoint);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _logger.LogInformation($"Processed Vehicle Identification Document with VIN: {response.Document.Inference.Prediction.Fields["vin"].Last().Values.Last().GetRawText()}");

            return response.Document.Inference.Prediction;

            //var mockGeneratedV1Document = new GeneratedV1Document
            //{
            //    Fields = new Dictionary<string, GeneratedFeature>
            //    {
            //        { "vin", new GeneratedFeature(false) { new GeneratedObject { { "value", JsonDocument.Parse("\"1HGCM82633A123456\"").RootElement } } } },
            //        { "make", new GeneratedFeature(false) { new GeneratedObject { { "value", JsonDocument.Parse("\"Honda\"").RootElement } } } },
            //        { "model", new GeneratedFeature(false) { new GeneratedObject { { "value", JsonDocument.Parse("\"Accord\"").RootElement } } } },
            //        { "year", new GeneratedFeature(false) { new GeneratedObject { { "value", JsonDocument.Parse("\"2023\"").RootElement } } } }
            //    }
            //};

            //_logger.LogInformation($"Processed Vehicle Identification Document with VIN: {mockGeneratedV1Document.Fields["vin"].Last().Values.Last().GetRawText()}");

            //return mockGeneratedV1Document;
        }
    }
}
