using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Services
{
    public class MqttSubscriptionManager : IMqttSubscriptionManager
    {
        private readonly IMqttConnectionManager _connectionManager;
        private readonly IContentManager _contentManager;
        private readonly ILogger<MqttSubscriptionManager> _logger;

        public MqttSubscriptionManager(
            IMqttConnectionManager connectionManager,
            IContentManager contentManager,
            ILogger<MqttSubscriptionManager> logger)
        {
            _connectionManager = connectionManager;
            _contentManager = contentManager;
            _logger = logger;
        }

        public async Task SubscribeAsync(string topicItemId, string topicToSubscribe)
        {
            var topicItem = await _contentManager.GetAsync(topicItemId);
            if (topicItem == null)
            {
                _logger.LogWarning("Subscribe: Could not find Topic ContentItem with ID {TopicId}", topicItemId);
                return;
            }

            var topicPart = topicItem.As<TopicPart>();
            if (topicPart == null)
            {
                _logger.LogWarning("Subscribe: ContentItem {TopicId} does not have a TopicPart", topicItemId);
                return;
            }
            
            var brokerId = topicPart.Broker?.ContentItemIds?.FirstOrDefault();
            if (string.IsNullOrEmpty(brokerId))
            {
                _logger.LogWarning("Subscribe: Topic {TopicId} is not associated with a Broker", topicItemId);
                return;
            }

            _logger.LogInformation("Requesting subscription to '{Topic}' for Broker '{BrokerId}'.", topicToSubscribe, brokerId);
            await _connectionManager.SubscribeAsync(brokerId, topicToSubscribe);
            _logger.LogInformation("Subscription request for '{Topic}' completed for Broker '{BrokerId}'.", topicToSubscribe, brokerId);
        }

        public async Task UnsubscribeAsync(string topicItemId)
        {
            var topicItem = await _contentManager.GetAsync(topicItemId);
            if (topicItem == null)
            {
                _logger.LogWarning("Unsubscribe: Could not find Topic ContentItem with ID {TopicId}", topicItemId);
                return;
            }

            var topicPart = topicItem.As<TopicPart>();
            if (topicPart == null)
            {
                _logger.LogWarning("Unsubscribe: ContentItem {TopicId} does not have a TopicPart", topicItemId);
                return;
            }

            var topicToUnsubscribe = topicPart.TopicPattern?.Text;
            if (string.IsNullOrEmpty(topicToUnsubscribe))
            {
                _logger.LogWarning("Unsubscribe: Topic {TopicId} does not have a topic pattern defined", topicItemId);
                return;
            }
            
            var brokerId = topicPart.Broker?.ContentItemIds?.FirstOrDefault();
            if (string.IsNullOrEmpty(brokerId))
            {
                _logger.LogWarning("Unsubscribe: Topic {TopicId} is not associated with a Broker", topicItemId);
                return;
            }

            _logger.LogInformation("Requesting to unsubscribe from '{Topic}' for Broker '{BrokerId}'.", topicToUnsubscribe, brokerId);
            await _connectionManager.UnsubscribeAsync(brokerId, topicToUnsubscribe);
            _logger.LogInformation("Unsubscribe request for '{Topic}' completed for Broker '{BrokerId}'.", topicToUnsubscribe, brokerId);
        }

        public async Task<IReadOnlyList<string>> ListSubscriptionsAsync(string brokerItemId)
        {
            return await _connectionManager.GetSubscriptionsAsync(brokerItemId);
        }

        public Task<long> GetMessageStatsAsync(string brokerItemId)
        {
            throw new System.NotImplementedException();
        }
    }
}
