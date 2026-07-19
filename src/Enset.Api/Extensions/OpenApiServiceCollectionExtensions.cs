using Microsoft.OpenApi;

namespace Enset.Api.Extensions;

public static class OpenApiServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiServices(
        this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "ENSET Data Lake House API",
                    Version = "v1",
                    Description =
                        "REST API des ENSET Data Lake House für Importprozesse, " +
                        "Stammdaten, Analysen und zukünftige Data Products."
                });
        });

        return services;
    }
}
/*
Verantwortung

Swagger

OpenAPI

XML Comments

Version

Tags
*/