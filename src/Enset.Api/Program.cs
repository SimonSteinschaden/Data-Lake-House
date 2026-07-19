using Enset.Api.Extensions;
using Enset.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiServices()
    .AddOpenApiServices()
    .AddImportServices(builder.Environment);

var connectionString = builder.Configuration.GetConnectionString(
    "EnsetDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'EnsetDatabase' is not configured.");

builder.Services.AddDbPersistence(connectionString);

var app = builder.Build();

app.UseApiPipeline();

app.MapControllers();

app.Run();

public partial class Program;
