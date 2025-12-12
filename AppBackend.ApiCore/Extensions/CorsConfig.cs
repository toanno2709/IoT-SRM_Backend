using Microsoft.Extensions.DependencyInjection;

namespace AppBackend.Extensions;

public static class CorsConfig
{
    public static IServiceCollection AddCorsConfig(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            // Production CORS policy - Specific origins only
            options.AddPolicy("AllowSpecificOrigins",
                builder =>
                {
                    builder.WithOrigins(
                            "https://iot-showroom.vercel.app",      // Production Vercel
                            "https://iot-showroom.vercel.app/",     // Production Vercel with trailing slash
                            "https://motosafe.site",                // Production domain
                            "https://motosafe.site/",               // Production domain with trailing slash
                            "http://localhost:3000",                // Local React dev
                            "http://localhost:5173",                // Local Vite dev
                            "http://localhost:4200",                // Local Angular dev
                            "https://localhost:7070",               // Local .NET HTTPS
                            "http://localhost:5000"                 // Local .NET HTTP
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetIsOriginAllowedToAllowWildcardSubdomains();
                });

            // Development CORS policy - For local development with credentials support
            options.AddPolicy("AllowAllOrigins",
                builder =>
                {
                    builder.SetIsOriginAllowed(origin => true) // Allow any origin in development
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials(); // Support credentials for SignalR
                });
        });
        return services;
    }
}
