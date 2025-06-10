using Adapters.Common.SBO.Repositories;
using Adapters.Common.SBO.Services;
using Adapters.CrossPlatform.SBO;
using Adapters.CrossPlatform.SBO.Services;
using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Service.Configuration;

public static class SboServiceLayerDependencyInjection {
    public static void ConfigureServices(IServiceCollection services) {
        // External System Adapters for SBO 9.0
        services.AddSingleton<SboCompany>();
        services.AddScoped<SboDatabaseService>();
        services.AddScoped<SboEmployeeRepository>();
        services.AddScoped<SboGeneralRepository>();
        services.AddScoped<SboItemRepository>();
        services.AddScoped<SboPickingRepository>();
        services.AddScoped<SboInventoryCountingRepository>();
        services.AddScoped<SboGoodsReceiptRepository>();
        services.AddScoped<IExternalSystemAdapter, SboServiceLayerAdapter>();
    }
}