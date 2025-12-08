using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AmazData.Module.Mqtt.Services;
using AmazData.Module.Mqtt.Models;

namespace AmazData.Module.Mqtt.BackgroundServices
{
    public class MqttEventService : IHostedService
    {
        private readonly ILogger<MqttEventService> _logger;
        private readonly IMqttConnectionManager _mqttConnectionManager;
        private readonly IServiceScopeFactory _scopeFactory;

        public MqttEventService(
            ILogger<MqttEventService> logger,
            IMqttConnectionManager mqttConnectionManager,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _mqttConnectionManager = mqttConnectionManager;
            _scopeFactory = scopeFactory;
            StartAsync(CancellationToken.None);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MQTT Event Service is starting.");
            _mqttConnectionManager.OnMessageReceived += HandleMessageReceivedAsync;
            return Task.CompletedTask;
        }

        private async void HandleMessageReceivedAsync(object? sender, BrokerMessageEventArgs e)
        {
            _logger.LogDebug("Received MQTT message via event. Topic: {Topic}", e.Topic);

            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var brokerService = scope.ServiceProvider.GetRequiredService<IBrokerService>();
                    await brokerService.CreateMessageRecordsAsync(e.ConnectionKey, e.Topic, e.Payload);
                    _logger.LogInformation("Successfully created message record for topic {Topic}", e.Topic);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing MQTT message from event for topic {Topic}", e.Topic);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MQTT Event Service is stopping.");
            _mqttConnectionManager.OnMessageReceived -= HandleMessageReceivedAsync;
            return Task.CompletedTask;
        }
    }
}
