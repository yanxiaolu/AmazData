using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Services
{
    public class MqttSubscriptionManager : IMqttSubscriptionManager
    {
        private static readonly Action<ILogger, string, Exception?> _logSubscribeTopicItemNotFound =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(1, nameof(SubscribeAsync)),
                "Subscribe: Could not find Topic ContentItem with ID {TopicId}");

        private static readonly Action<ILogger, string, Exception?> _logSubscribeTopicPartMissing =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(2, nameof(SubscribeAsync)),
                "Subscribe: ContentItem {TopicId} does not have a TopicPart");

        private static readonly Action<ILogger, string, Exception?> _logSubscribeBrokerMissing =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(3, nameof(SubscribeAsync)),
                "Subscribe: Topic {TopicId} is not associated with a Broker");

        private static readonly Action<ILogger, string, string, Exception?> _logSubscribeRequesting =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(4, nameof(SubscribeAsync)),
                "Requesting subscription to '{Topic}' for Broker '{BrokerId}'.");

        private static readonly Action<ILogger, string, string, Exception?> _logSubscribeCompleted =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(5, nameof(SubscribeAsync)),
                "Subscription request for '{Topic}' completed for Broker '{BrokerId}'.");

        private static readonly Action<ILogger, string, Exception?> _logUnsubscribeTopicItemNotFound =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(6, nameof(UnsubscribeAsync)),
                "Unsubscribe: Could not find Topic ContentItem with ID {TopicId}");

        private static readonly Action<ILogger, string, Exception?> _logUnsubscribeTopicPartMissing =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(7, nameof(UnsubscribeAsync)),
                "Unsubscribe: ContentItem {TopicId} does not have a TopicPart");

        private static readonly Action<ILogger, string, Exception?> _logUnsubscribeTopicPatternMissing =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(8, nameof(UnsubscribeAsync)),
                "Unsubscribe: Topic {TopicId} does not have a topic pattern defined");

        private static readonly Action<ILogger, string, Exception?> _logUnsubscribeBrokerMissing =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(9, nameof(UnsubscribeAsync)),
                "Unsubscribe: Topic {TopicId} is not associated with a Broker");

        private static readonly Action<ILogger, string, string, Exception?> _logUnsubscribeRequesting =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(10, nameof(UnsubscribeAsync)),
                "Requesting to unsubscribe from '{Topic}' for Broker '{BrokerId}'.");

        private static readonly Action<ILogger, string, string, Exception?> _logUnsubscribeCompleted =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(11, nameof(UnsubscribeAsync)),
                "Unsubscribe request for '{Topic}' completed for Broker '{BrokerId}'.");

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
                _logSubscribeTopicItemNotFound(_logger, topicItemId, null);
                return;
            }

            var topicPart = topicItem.As<TopicPart>();
            if (topicPart == null)
            {
                _logSubscribeTopicPartMissing(_logger, topicItemId, null);
                return;
            }
            
            var brokerId = topicPart.Broker?.ContentItemIds?.FirstOrDefault();
            if (string.IsNullOrEmpty(brokerId))
            {
                _logSubscribeBrokerMissing(_logger, topicItemId, null);
                return;
            }

            _logSubscribeRequesting(_logger, topicToSubscribe, brokerId, null);
            await _connectionManager.SubscribeAsync(brokerId, topicToSubscribe);
            _logSubscribeCompleted(_logger, topicToSubscribe, brokerId, null);
        }

        public async Task UnsubscribeAsync(string topicItemId)
        {
            var topicItem = await _contentManager.GetAsync(topicItemId);
            if (topicItem == null)
            {
                _logUnsubscribeTopicItemNotFound(_logger, topicItemId, null);
                return;
            }

            var topicPart = topicItem.As<TopicPart>();
            if (topicPart == null)
            {
                _logUnsubscribeTopicPartMissing(_logger, topicItemId, null);
                return;
            }

            var topicToUnsubscribe = topicPart.TopicPattern?.Text;
            if (string.IsNullOrEmpty(topicToUnsubscribe))
            {
                _logUnsubscribeTopicPatternMissing(_logger, topicItemId, null);
                return;
            }
            
            var brokerId = topicPart.Broker?.ContentItemIds?.FirstOrDefault();
            if (string.IsNullOrEmpty(brokerId))
            {
                _logUnsubscribeBrokerMissing(_logger, topicItemId, null);
                return;
            }

            _logUnsubscribeRequesting(_logger, topicToUnsubscribe, brokerId, null);
            await _connectionManager.UnsubscribeAsync(brokerId, topicToUnsubscribe);
            _logUnsubscribeCompleted(_logger, topicToUnsubscribe, brokerId, null);
        }

        public async Task<IReadOnlyList<string>> ListSubscriptionsAsync(string brokerItemId)
        {
            return await _connectionManager.GetSubscriptionsAsync(brokerItemId);
        }
    }
}
