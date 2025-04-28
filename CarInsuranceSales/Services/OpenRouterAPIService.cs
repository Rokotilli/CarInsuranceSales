using CarInsuranceSales.Interfaces;
using CarInsuranceSales.Models;
using Microsoft.Extensions.Options;
using Mindee.Product.Generated;
using Mindee.Product.InternationalId;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace CarInsuranceSales.Services
{
    public class OpenRouterAPIService : IOpenRouterAPIService
    {
        private readonly HttpClient _httpClient;
        private readonly Config _config;

        public OpenRouterAPIService(IOptions<Config> options, IHttpClientFactory httpClientFactory)
        {
            _config = options.Value;
            _httpClient = httpClientFactory.CreateClient(_config.OpenRouterAPI.Name);
        }

        public async Task<OpenRouterAPIResponseModel> GetMessageAsync(string message)
        {
            var requestBody = new
            {
                model = "mistralai/mistral-small-24b-instruct-2501:free",
                messages = new[]
                {
                    new { role = "user", content = message }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error in OpenRouterAPIService method GetMessageAsync: {response.StatusCode}");
            }
            
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<OpenRouterAPIResponseModel>(responseString);

            return result;
        }
        
        public async Task<OpenRouterAPIResponseModel> GetInsurancePolicyAsync(InternationalIdV2Document internationalPassportDocument, GeneratedV1Document technicalPassportDocument)
        {
            var requestBody = new
            {
                model = "mistralai/mistral-small-24b-instruct-2501:free",
                messages = new[]
                {
                    new { role = "user", content = _config.Messages.GeneratedInsurancePolicyDocumentMessage +
                    "International passport:" + JsonConvert.SerializeObject(internationalPassportDocument) +
                    "And Vehicle Information:" +
                    $"VIN: {technicalPassportDocument.Fields["vehicle_identification_number"].Last().Values.Last().GetRawText()}\n" +
                    $"Manufacturer: {technicalPassportDocument.Fields["manufacturer"].Last().Values.Last().GetRawText()}\n" +
                    $"Model: {technicalPassportDocument.Fields["model"].Last().Values.Last().GetRawText()}\n" +
                    $"Color: {Regex.Unescape(technicalPassportDocument.Fields["color"].Last().Values.Last().GetRawText())}"}
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error in OpenRouterAPIService method GetInsurancePolicyAsync: {response.StatusCode}");
            }

            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<OpenRouterAPIResponseModel>(responseString);

            return result;
        }
    }
}
