using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using AmazData.Module.PlcStat.Services;

namespace AmazData.Module.PlcStat;

/// <summary>
/// 模块启动配置类
/// 负责注册模块所需的服务和配置路由
/// </summary>
public sealed class Startup : StartupBase
{
    /// <summary>
    /// 配置依赖注入服务
    /// </summary>
    /// <param name="services">服务集合</param>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPostgreSqlConnectionProvider, PostgreSqlConnectionProvider>();
        // 注册 PLC 数据仓储服务
        services.AddScoped<IPlcDataRepository, PlcDataRepository>();
    }

    /// <summary>
    /// 配置请求处理管道和路由
    /// </summary>
    /// <param name="builder">应用构建器</param>
    /// <param name="routes">路由构建器</param>
    /// <param name="serviceProvider">服务提供者</param>
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