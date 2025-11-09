using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using OrchardCore.BackgroundTasks;
using OrchardCore.ContentManagement;
using OrchardCore.Entities;

namespace AmazData.Module.Mqtt.Services
{
    public class MqttBackgroundService : IBackgroundTask
    {
        private readonly ILogger<MqttBackgroundService> _logger;
        public MqttBackgroundService(ILogger<MqttBackgroundService> logger)
        {
            _logger = logger;
        }

        public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            _logger.LogDebug("MQTT Background Task running a check.");



        }
    }
}
