using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Enset.Api.Authorization;
using Enset.Application.Authorization;
using Enset.Domain.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Enset.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
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

        services.AddScoped<CurrentUserContext>();
        services.AddScoped<ICurrentUserContext>(provider =>
            provider.GetRequiredService<CurrentUserContext>());

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");
        if (string.IsNullOrWhiteSpace(jwt.Issuer) ||
            string.IsNullOrWhiteSpace(jwt.Audience) ||
            jwt.SigningKey.Length < 32)
            throw new InvalidOperationException("JWT issuer, audience and a signing key of at least 32 characters are required.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<JwtCurrentUserEvents>();
        services.AddScoped<DevelopmentTokenService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
                options.EventsType = typeof(JwtCurrentUserEvents);
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicyNames.Authenticated,
                policy => policy.RequireAuthenticatedUser());
            options.AddPolicy(AuthorizationPolicyNames.EnsetEmployee,
                policy => policy.RequireRole(
                    GlobalUserRole.EnsetEmployee.ToString(),
                    GlobalUserRole.EnsetAdmin.ToString()));
            options.AddPolicy(AuthorizationPolicyNames.EnsetAdmin,
                policy => policy.RequireRole(GlobalUserRole.EnsetAdmin.ToString()));
            options.AddPolicy(AuthorizationPolicyNames.CustomerReader,
                policy => policy.RequireRole(
                    GlobalUserRole.EnsetEmployee.ToString(),
                    GlobalUserRole.EnsetAdmin.ToString(),
                    UserCustomerRole.CustomerAdmin.ToString(),
                    UserCustomerRole.CustomerUser.ToString(),
                    UserCustomerRole.CustomerViewer.ToString()));
            options.AddPolicy(AuthorizationPolicyNames.CustomerWriter,
                policy => policy.RequireRole(
                    GlobalUserRole.EnsetEmployee.ToString(),
                    GlobalUserRole.EnsetAdmin.ToString(),
                    UserCustomerRole.CustomerAdmin.ToString(),
                    UserCustomerRole.CustomerUser.ToString()));
            options.AddPolicy(AuthorizationPolicyNames.CustomerAdmin,
                policy => policy.RequireRole(
                    GlobalUserRole.EnsetEmployee.ToString(),
                    GlobalUserRole.EnsetAdmin.ToString(),
                    UserCustomerRole.CustomerAdmin.ToString()));
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
