using Mindee.Product.Generated;
using Mindee.Product.InternationalId;
using Telegram.Bot.Types;

namespace CarInsuranceSales.Interfaces
{
    public interface IMindeeAPIService
    {
        Task<InternationalIdV2Document> ProcessInternationalIdAsync(PhotoSize photo);

        Task<GeneratedV1Document> ProcessVehicleIdentificationDocumentAsync(PhotoSize photo);
    }
}
