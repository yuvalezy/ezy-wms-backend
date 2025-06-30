using System.Text.Json;
using Adapters.CrossPlatform.SBO.Services;

namespace UnitTests.Integration.ExternalSystems.Shared;

public static class SboDataHelper {
    public static async Task<string> GetCustomer(SboCompany sboCompany) {
        const string url = "BusinessPartners?$select=CardCode,CardName,CardType,Valid&$filter=CardType eq 'cCustomer' and Currency eq 'EUR'&$top=1";

        var response = await sboCompany.GetAsync<JsonDocument>(url);
        if (response == null) {
            throw new Exception("Failed to get customer");
        }

        var value = response.RootElement.GetProperty("value");
        return value.EnumerateArray()
            .First()
            .GetProperty("CardCode")
            .GetString() ?? throw new InvalidOperationException("Failed to get customer");
    }
}