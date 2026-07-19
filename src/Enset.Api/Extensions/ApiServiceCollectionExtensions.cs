using System.Text.Json.Serialization;

namespace Enset.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            });

        services.AddProblemDetails();

        return services;
    }
}
/*
Verantwortung

AddControllers()

JSON

ProblemDetails
*/