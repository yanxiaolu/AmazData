using AmazData.Module.Mqtt.Drivers;
using AmazData.Module.Mqtt.Migrations;
using AmazData.Module.Mqtt.Models;
using AmazData.Module.Mqtt.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Users.Models;

namespace AmazData.Module.Mqtt;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IMqttOptionsBuilderService, MqttOptionsBuilderService>();
        services.AddScoped<IMqttSubscriptionManager, MqttSubscriptionManager>();
        services.AddSingleton<IMqttConnectionManager, MqttConnectionManager>();
        services.AddDataMigration<MqttMigrations>();
        services.AddContentPart<BrokerPart>();
        services.AddContentPart<TopicPart>();
        services.AddContentPart<DataRecordPart>();
        services.AddScoped<IDisplayDriver<User>, AmazDataMqttUserButtonDisplayDriver>();
        // 注册 Content Display Driver 以添加按钮
        services.AddScoped<IContentDisplayDriver, MqttBrokerButtonsDisplayDriver>();
        services.AddScoped<IContentDisplayDriver, MqttTopicButtonsDisplayDriver>();

    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: "Subscription",
            areaName: "AmazData.Module.Mqtt",
            pattern: "Subscription/{action}/{id?}",
            defaults: new { controller = "Subscription" }
        );

        routes.MapAreaControllerRoute(
            name: "Home",
            areaName: "AmazData.Module.Mqtt",
            pattern: "Home/Index",
            defaults: new { controller = "Home", action = "Index" }
        );
    }
}

