using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using AmazData.Module.PlcStat.Services;

namespace AmazData.Module.PlcStat;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPostgreSqlConnectionProvider, PostgreSqlConnectionProvider>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes,
        IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: "Home",
            areaName: "AmazData.Module.PlcStat",
            pattern: "Home/Index",
            defaults: new { controller = "PlcData", action = "Index" }
        );
    }
}