using Mindee.Product.Generated;
using Mindee.Product.InternationalId;

namespace CarInsuranceSales.Interfaces
{
    public interface IMindeeAPIService
    {
        Task<InternationalIdV2Document> ProcessInternationalIdAsync(string filePath);

        Task<GeneratedV1Document> ProcessVehicleIdentificationDocumentAsync(string filePath);
    }
}
