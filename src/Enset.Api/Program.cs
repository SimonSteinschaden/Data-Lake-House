using Enset.Api.Extensions;
using Enset.Infrastructure.Persistence;
using Enset.Infrastructure.DataProducts;

var builder = WebApplication.CreateBuilder(args);

const string developmentFrontendCorsPolicy = "DevelopmentFrontend";

builder.Services
    .AddApiServices(builder.Environment)
    .AddOpenApiServices()
    .AddImportServices(builder.Environment);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            developmentFrontendCorsPolicy,
            policy => policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}

var connectionString = builder.Configuration.GetConnectionString(
    "EnsetDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'EnsetDatabase' is not configured.");

builder.Services.AddDbPersistence(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    //await app.Services.SeedDataProductDemoAsync();
    app.UseCors(developmentFrontendCorsPolicy);
}

app.UseApiPipeline();

app.MapControllers();

app.Run();

public partial class Program;
