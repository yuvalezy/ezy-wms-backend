using Microsoft.Extensions.DependencyInjection;

namespace Service;

public static class DependencyInjectionConfig {
    public static IServiceCollection ConfigureServices(this IServiceCollection services, Settings settings) {
        services.AddSingleton<ISessionManager>(_ => new InMemorySessionManager());
        return services;
    }
}