using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;

namespace Enset.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            });

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                if (!environment.IsDevelopment())
                    return;

                var exception = context.HttpContext.Features
                    .Get<IExceptionHandlerFeature>()?.Error;
                if (exception is null)
                    return;

                context.ProblemDetails.Detail = exception.Message;
                context.ProblemDetails.Extensions["exception"] = exception.ToString();
            };
        });

        return services;
    }
}
/*
Verantwortung

AddControllers()

JSON

ProblemDetails
*/
