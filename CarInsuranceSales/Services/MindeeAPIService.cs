using CarInsuranceSales.Interfaces;
using Mindee;
using Mindee.Parsing.Generated;
using Mindee.Parsing.Standard;
using Mindee.Product.Generated;
using Mindee.Product.InternationalId;
using System.Text.Json;

namespace CarInsuranceSales.Services
{
    public class MindeeAPIService : IMindeeAPIService
    {
        private readonly MindeeClient _mindeeClient;

        public MindeeAPIService(MindeeClient mindeeClient)
        {
            _mindeeClient = mindeeClient;
        }

        public async Task<InternationalIdV2Document> ProcessInternationalIdAsync(string filePath)
        {
            //var inputSource = new LocalInputSource(filePath);

            //var response = await _mindeeClient.EnqueueAndParseAsync<InternationalIdV2>(inputSource);

            //if (File.Exists(filePath))
            //{
            //    File.Delete(filePath);
            //}

            var mockInternationalIdV2Document = new InternationalIdV2Document
            {
                GivenNames = new List<StringField>
                {
                    new StringField("John", "John", 0.99, null, null),
                    new StringField("Doe", "Doe", 0.99, null, null)
                },
                DocumentNumber = new StringField("A12345678", "A12345678", 0.99, null, null),
                ExpiryDate = new DateField("2030-01-01", 0.99, null, null, false)
            };


            return mockInternationalIdV2Document;
        }

        public async Task<GeneratedV1Document> ProcessVehicleIdentificationDocumentAsync(string filePath)
        {
            //var inputSource = new LocalInputSource(filePath);

            //var endpoint = new CustomEndpoint(
            //    endpointName: "vehicle_identification_document",
            //    accountName: "Rokotilli",
            //    version: "1"
            //);

            //var response = await _mindeeClient.EnqueueAndParseAsync<GeneratedV1>(inputSource, endpoint);

            //if (File.Exists(filePath))
            //{
            //    File.Delete(filePath);
            //}

            var mockGeneratedV1Document = new GeneratedV1Document
            {
                Fields = new Dictionary<string, GeneratedFeature>
                {
                    { "vin", new GeneratedFeature(false) { new GeneratedObject { { "value", JsonDocument.Parse("\"1HGCM82633A123456\"").RootElement } } } },
                    { "make", new GeneratedFeature(false) { new GeneratedObject { { "value", JsonDocument.Parse("\"Honda\"").RootElement } } } },
                    { "model", new GeneratedFeature(false) { new GeneratedObject { { "value", JsonDocument.Parse("\"Accord\"").RootElement } } } },
                    { "year", new GeneratedFeature(false) { new GeneratedObject { { "value", JsonDocument.Parse("\"2023\"").RootElement } } } }
                }
            };

            return mockGeneratedV1Document;
        }
    }
}
