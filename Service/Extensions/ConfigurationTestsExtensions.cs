using System;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Models.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Service.Extensions;

public static class ConfigurationTestsExtensions {
    public async static Task<WebApplication> TestConfigurations(this WebApplication app, Settings settings) {
        using var scope = app.Services.CreateScope();
        var adapter = scope.ServiceProvider.GetRequiredService<IExternalSystemAdapter>();

        if (!await adapter.ValidateUserDefinedFieldAsync("OPKL", "WMS_READY")) {
            throw new InvalidOperationException(
                "Required SAP Business One user-defined field 'U_WMS_READY' is missing on table 'OPKL'. " +
                "Please create the UDF in SAP Business One (Tools -> Customization Tools -> User-Defined Fields - Management) " +
                "on the Pick List header (OPKL) with name 'WMS_READY' before starting the service.");
        }
        return app;
    }
}
