using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.BackgroundTasks;
using AmazData.Module.Mqtt.Services;

namespace AmazData.Module.Mqtt
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IBackgroundTask, MqttBackgroundService>();
        }
    }
}