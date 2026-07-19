namespace Enset.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiPipeline(
        this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(
                    "/swagger/v1/swagger.json",
                    "ENSET Data Lake House API v1");
            });
        }

        return app;
    }
}
/*
Verantwortung


UseExceptionHandler()

UseAuthentication()

UseAuthorization()

UseHttpsRedirection()

...
*/