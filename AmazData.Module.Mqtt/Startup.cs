using AmazData.Module.Mqtt.BackgroundServices;
using AmazData.Module.Mqtt.Drivers;
using AmazData.Module.Mqtt.Migrations;
using AmazData.Module.Mqtt.Models;
using AmazData.Module.Mqtt.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.BackgroundTasks;
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

        //添加内容类型
        services.AddContentPart<BrokerPart>();
        services.AddContentPart<TopicPart>();
        services.AddContentPart<DataRecordPart>();
        services.AddDataMigration<MqttMigrations>();
        // Scoped
        services.AddScoped<IBrokerService, BrokerService>();
        services.AddScoped<IMqttSubscriptionManager, MqttSubscriptionManager>();
        // Singleton
        services.AddSingleton<IMqttConnectionManager, MqttConnectionManager>();
        // 【新增】注册消息通道 (Singleton)
        services.AddSingleton<MqttMessageChannel>();

        // 注册 Content Display Driver 以添加按钮
        services.AddScoped<IContentDisplayDriver, MqttBrokerButtonsDisplayDriver>();
        services.AddScoped<IContentDisplayDriver, MqttTopicButtonsDisplayDriver>();
        services.AddScoped<IDisplayDriver<User>, AmazDataMqttUserButtonDisplayDriver>();

        // 注册后台任务
        services.AddScoped<IBackgroundTask, MqttMessageProcessor>();

    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: "Subscription",
            areaName: "AmazData.Module.Mqtt",
            pattern: "Subscription/{action}/{id?}",
            defaults: new { controller = "MqttTopic" }
        );

        routes.MapAreaControllerRoute(
            name: "ConnectBroker",
            areaName: "AmazData.Module.Mqtt",
            pattern: "MqttBroker/ConnectBroker/{id?}",
            defaults: new { controller = "MqttBroker", action = "ConnectBroker" }
        );
        routes.MapAreaControllerRoute(
            name: "HomeTest",
            areaName: "AmazData.Module.Mqtt",
            pattern: "Home/Test",
            defaults: new { controller = "MqttBroker", action = "Test" }
        );
    }
}
