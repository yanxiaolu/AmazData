using Microsoft.Extensions.Logging;
using OrchardCore.BackgroundTasks;

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
