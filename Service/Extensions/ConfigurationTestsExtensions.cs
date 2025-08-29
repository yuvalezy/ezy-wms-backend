using System.ComponentModel.DataAnnotations;
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
            throw new ValidationException($"Settings -> Filters -> PickReady is not valid.");       
        }
        return app;
    }
}