using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Services
{
    public class MqttSubscriptionManager : IMqttSubscriptionManager
    {

        private readonly IMqttConnectionManager _connectionManager;
        private readonly ILogger<MqttSubscriptionManager> _logger;

        public MqttSubscriptionManager(IMqttConnectionManager connectionManager, ILogger<MqttSubscriptionManager> logger)
        {
            _connectionManager = connectionManager;
            _logger = logger;
        }

        public async Task SubscribeAsync(string topicItemId, string topicToSubscribe)
        {
            var connectionId = topicItemId; // topicItemId 就是 connectionId

            _logger.LogInformation("Requesting subscription to '{Topic}' for connection '{ConnectionId}'.", topicToSubscribe, connectionId);

            // 将所有复杂逻辑委托给 ConnectionManager
            await _connectionManager.AddSubscriptionAsync(connectionId, topicToSubscribe);

            _logger.LogInformation("Subscription request for '{Topic}' completed for connection '{ConnectionId}'.", topicToSubscribe, connectionId);
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
