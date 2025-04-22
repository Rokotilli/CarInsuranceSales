using CarInsuranceSales.Models;
using Mindee.Product.Generated;
using Mindee.Product.InternationalId;

namespace CarInsuranceSales.Interfaces
{
    public interface IOpenRouterAPIService
    {
        Task<OpenRouterAPIResponseModel> GetMessageAsync(string message);
        Task<OpenRouterAPIResponseModel> GetInsurancePolicyAsync(InternationalIdV2Document internationalPassportDocument, GeneratedV1Document technicalPassportDocument);
    }
}
