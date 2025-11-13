using System.Text;
using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Services
{
    public class MqttSubscriptionManager : IMqttSubscriptionManager
    {
        private readonly IContentManager _contentManager;
        private readonly IMqttConnectionManager _connectionManager;
        private readonly ILogger<MqttSubscriptionManager> _logger;

        public MqttSubscriptionManager(
            IContentManager contentManager,
            IMqttConnectionManager connectionManager,
            ILogger<MqttSubscriptionManager> logger)
        {
            _contentManager = contentManager;
            _connectionManager = connectionManager;
            _logger = logger;
        }

        public async Task SubscribeAsync(string topicItemId)
        {
            // 根据你依赖的版本，这里使用 MqttFactory 更常见、兼容性好
            var factory = new MqttClientFactory();
            var mqttClient = factory.CreateMqttClient();

            // 消息到达时的处理器
            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var payload = e.ApplicationMessage?.Payload == null ? string.Empty : Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                // 移除 payload 中的换行符，确保单行输出
                var sanitizedPayload = payload.Replace("\n", " ").Replace("\r", " ");
                _logger.LogInformation($"[{DateTime.Now:O}] Topic: {e.ApplicationMessage?.Topic} QoS: {e.ApplicationMessage?.QualityOfServiceLevel} Payload: {sanitizedPayload} {new string('-', 60)}");
                return Task.CompletedTask;
            };

            // 连接/重连成功后订阅（放在这里能在自动重连后再次订阅）
            mqttClient.ConnectedAsync += async e =>
            {
                _logger.LogInformation("MQTT 已连接。订阅主题...");
                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("system/MonitorData").WithAtLeastOnceQoS().Build());
                _logger.LogInformation("订阅完成：system/MonitorData");
            };

            mqttClient.DisconnectedAsync += e =>
            {
                _logger.LogInformation($"MQTT 已断开（reason: {e.Reason}）。异常: {e.Exception?.Message}");
                return Task.CompletedTask;
            };

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("8.152.96.245") // 替换为你的 broker
                                               // 可选：.WithCleanSession(false) 来保持会话（有些 broker/用例）
                .Build();

            await mqttClient.ConnectAsync(options, CancellationToken.None);
        }

        public Task UnsubscribeAsync(string topicItemId)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<string>> ListSubscriptionsAsync(string brokerItemId)
        {
            throw new NotImplementedException();
        }

        public Task<long> GetMessageStatsAsync(string brokerItemId)
        {
            throw new NotImplementedException();
        }
    }
}
