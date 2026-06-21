namespace Hikr.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // Use Global Exception Handler
        app.UseExceptionHandler();

        // Use CORS
        app.UseCors("AllowAll");

        // Swagger
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hikr.Api v1");
            });
        }

        // Health Checks
        app.MapHealthChecks("/health");

        return app;
    }
}
