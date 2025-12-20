using AmazData.Module.Yunmou.Models;
using AmazData.Module.Yunmou.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace AmazData.Module.Yunmou;

/// <summary>
/// 云眸模块启动配置类
/// </summary>
public sealed class Startup : StartupBase
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 注册模块服务
    /// </summary>
    public override void ConfigureServices(IServiceCollection services)
    {
        // 绑定云眸 API 配置
        services.Configure<YunMouSettings>(_configuration.GetSection("YunMou"));
        // 注册 HttpClient 客户端
        services.AddHttpClient<IYunMouApiClient, YunMouApiClient>();
    }

    /// <summary>
    /// 配置模块路由和中间件
    /// </summary>
    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes,
        IServiceProvider serviceProvider)
    {
        
    }
}