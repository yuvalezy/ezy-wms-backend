using Microsoft.Extensions.DependencyInjection;

namespace Service.Configuration;

public static class CorsConfig {
    
    public static IServiceCollection ConfigureCorsPolicies(this IServiceCollection services) {
        const string developmentOrigin = "AllowLocalhost";
        services.AddCors(options => {
            options.AddPolicy(developmentOrigin,
                policy => {
                    policy.WithOrigins(
                            "http://localhost:3000",
                            "http://localhost:3001",
                            "http://localhost:5173",
                            "http://localhost:3000/",
                            "http://localhost:3001/",
                            "http://localhost:5173/",
                            "https://ezyprop.vercel.app",
                            "https://ezyprop-app.vercel.app",
                            "http://localhost:5174/",
                            "https://www.ezyprop.com",
                            "https://www.app.ezyprop.com",
                            "https://app.ezyprop.com"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });
        return services;
    }
}