using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using OrchardCore.ContentManagement;
using OrchardCore.Entities;
using OrchardCore.Modules;
using OrchardCore.Settings;

namespace AmazData.Module.Mqtt.Services
{
    public class MqttBackgroundService : IBackgroundTask
    {
        private readonly ILogger<MqttBackgroundService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public MqttBackgroundService(
            ILogger<MqttBackgroundService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("MQTT Background Task running a check.");

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
                var mqttOptionsBuilderService = scope.ServiceProvider.GetRequiredService<IMqttOptionsBuilderService>();
                var mqttConnectionManager = scope.ServiceProvider.GetRequiredService<IMqttConnectionManager>();

                // Get all active brokers
                var brokerContentItems = await contentManager.Query<ContentItem, ContentItemIndex>()
                    .Where(x => x.ContentType == "Broker" && x.Published)
                    .ListAsync();

                foreach (var brokerContentItem in brokerContentItems)
                {
                    var brokerPart = brokerContentItem.As<BrokerPart>();
                    if (brokerPart == null || !brokerPart.IsEnabled) continue;

                    var brokerId = brokerContentItem.ContentItemId;

                    // Ensure client is connected
                    var (status, _) = await mqttConnectionManager.GetConnectionStatusAsync(brokerId);
                    if (status != ConnectionStatus.Connected)
                    {
                        _logger.LogInformation("Broker '{BrokerId}' is not connected. Attempting to connect.", brokerId);
                        var options = await mqttOptionsBuilderService.BuildClientOptionsAsync(brokerId);
                        if (options != null)
                        {
                            await mqttConnectionManager.ConnectAsync(brokerId, options);
                            (status, _) = await mqttConnectionManager.GetConnectionStatusAsync(brokerId); // Re-check status
                        }
                    }

                    if (status == ConnectionStatus.Connected)
                    {
                        // Get all topics associated with this broker that are enabled and published
                        var topicContentItems = await contentManager.Query<ContentItem, ContentItemIndex>()
                            .Where(x => x.ContentType == "Topic" && x.Published)
                            .ListAsync();

                        var topicsToSubscribe = new List<MqttTopicFilter>();

                        foreach (var topicContentItem in topicContentItems)
                        {
                            var topicPart = topicContentItem.As<TopicPart>();
                            if (topicPart == null || !topicPart.IsEnabled) continue;

                            // Check if this topic is associated with the current broker
                            if (topicPart.Broker.ContentItemIds.Contains(brokerId))
                            {
                                var topicPattern = topicPart.TopicPattern.Text;
                                if (!string.IsNullOrEmpty(topicPattern))
                                {
                                    // Fetch the Broker content item to get QoS level
                                    var brokerPartForQos = brokerContentItem.As<BrokerPart>();
                                    MqttQualityOfServiceLevel qosLevel = MqttQualityOfServiceLevel.AtLeastOnce; // Default to QoS 1

                                    if (brokerPartForQos != null && !string.IsNullOrEmpty(brokerPartForQos.Qos.Text) && int.TryParse(brokerPartForQos.Qos.Text, out int qosInt))
                                    {
                                        qosLevel = (MqttQualityOfServiceLevel)qosInt;
                                    }
                                    else
                                    {
                                        _logger.LogWarning("QoS level not configured or invalid for Broker '{BrokerId}'. Defaulting to QoS 1 for topic '{TopicPattern}'.", brokerId, topicPattern);
                                    }

                                    topicsToSubscribe.Add(new MqttTopicFilterBuilder()
                                        .WithTopic(topicPattern)
                                        .WithQualityOfServiceLevel(qosLevel)
                                        .Build());
                                }
                            }
                        }

                        // Update subscriptions via MqttConnectionManager
                        await mqttConnectionManager.UpdateSubscriptionsAsync(brokerId, topicsToSubscribe);
                    }
                }
            }
        }
    }
}
