using Serilog;

namespace Hikr.Api.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder AddSerilogLogging(this IHostBuilder host)
    {
        host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console());

        return host;
    }
}
