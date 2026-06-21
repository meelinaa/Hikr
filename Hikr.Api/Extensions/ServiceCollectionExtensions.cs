using Hikr.Infrastructure.Data;
using Hikr.Api.Infrastructure;
using Hikr.Application.Repositories;
using Hikr.Infrastructure.Repositories;
using Hikr.Application.Services;
using Hikr.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Hikr.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        // Health Checks
        services.AddHealthChecks();

        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Global Exception Handler
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<HikrDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                o => o.UseNetTopologySuite()));

        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IWaypointRepository, WaypointRepository>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRouteService, RouteService>();
        services.AddScoped<IWaypointService, WaypointService>();

        services.AddHttpClient<IOsrmService, OsrmService>();

        return services;
    }

    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory(geometryFactory));
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

        return services;
    }
}
