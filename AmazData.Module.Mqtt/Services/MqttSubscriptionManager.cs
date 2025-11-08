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
            var topicContentItem = await _contentManager.GetAsync(topicItemId);
            var topicPart = topicContentItem?.As<TopicPart>();
            if (topicPart == null)
            {
                _logger.LogWarning("Could not subscribe. Topic with ID '{TopicId}' not found.", topicItemId);
                return;
            }

            var brokerContentItemIds = topicPart.Broker.ContentItemIds;
            if (brokerContentItemIds == null || brokerContentItemIds.Length == 0)
            {
                _logger.LogWarning("Could not subscribe. Topic '{TopicId}' has no associated broker.", topicItemId);
                return;
            }

            var brokerId = brokerContentItemIds[0];
            var client = await _connectionManager.GetClientAsync(brokerId);

            if (client == null || !client.IsConnected)
            {
                _logger.LogWarning("Could not subscribe. MQTT client for broker '{BrokerId}' is not available or not connected.", brokerId);
                return;
            }

            // Fetch the Broker content item to get QoS level
            var brokerContentItem = await _contentManager.GetAsync(brokerId);
            var brokerPart = brokerContentItem?.As<BrokerPart>();
            if (brokerPart == null)
            {
                _logger.LogWarning("Could not subscribe. Broker with ID '{BrokerId}' not found or has no BrokerPart.", brokerId);
                return;
            }

            var qosText = brokerPart.Qos.Text; // Assuming Qos is a TextField
            MqttQualityOfServiceLevel qosLevel = MqttQualityOfServiceLevel.AtLeastOnce; // Default to QoS 1

            if (!string.IsNullOrEmpty(qosText) && int.TryParse(qosText, out int qosInt))
            {
                qosLevel = (MqttQualityOfServiceLevel)qosInt;
            }
            else
            {
                _logger.LogWarning("QoS level not configured or invalid for Broker '{BrokerId}'. Defaulting to QoS 0.", brokerId);
            }



            var topicPattern = topicPart.TopicPattern.Text;
            _logger.LogInformation("Topic '{TopicPattern}' for broker '{BrokerId}' is configured for subscription.", topicPattern, brokerId);
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
