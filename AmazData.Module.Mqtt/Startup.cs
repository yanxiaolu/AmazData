using AmazData.Module.Mqtt.Migrations;
using AmazData.Module.Mqtt.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;

namespace AmazData.Module.Mqtt;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddDataMigration<MqttMigrations>();
        services.AddContentPart<BrokerPart>();
        services.AddContentPart<TopicPart>();
        services.AddContentPart<DataRecordPart>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: "Home",
            areaName: "AmazData.Module.Mqtt",
            pattern: "Home/Index",
            defaults: new { controller = "Home", action = "Index" }
        );
    }
}

