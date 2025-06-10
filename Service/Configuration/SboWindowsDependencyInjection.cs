using Adapters.Common.SBO.Repositories;
using Adapters.Common.SBO.Services;
using Adapters.Windows.SBO;
using Adapters.Windows.SBO.Repositories;
using Adapters.Windows.SBO.Services;
using Adapters.Windows.SBO.Utils;
using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Service.Configuration;

public static class SboWindowsDependencyInjection {
    public static void ConfigureServices(IServiceCollection services) {
        // External System Adapters for SBO 9.0
        SboAssembly.RedirectAssembly();
        services.AddSingleton<SboCompany>();
        services.AddScoped<SboDatabaseService>();
        services.AddScoped<SboEmployeeRepository>();
        services.AddScoped<SboGeneralRepository>();
        services.AddScoped<SboItemRepository>();
        services.AddScoped<SboPickingRepository>();
        services.AddScoped<SboInventoryCountingRepository>();
        services.AddScoped<SboGoodsReceiptRepository>();
        services.AddScoped<IExternalSystemAdapter, SboAdapter>();
    }
}