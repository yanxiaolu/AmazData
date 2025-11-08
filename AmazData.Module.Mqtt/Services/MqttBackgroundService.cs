using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using OrchardCore.ContentManagement;
using OrchardCore.Entities;
using OrchardCore.Settings;

namespace AmazData.Module.Mqtt.Services
{
    public class MqttBackgroundService : BackgroundService
    {
        private readonly ILogger<MqttBackgroundService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMqttConnectionManager _mqttConnectionManager;

        // This dictionary will hold the MQTT clients and their associated subscribed topics
        private readonly Dictionary<string, IMqttClient> _activeMqttClients = new();
        private readonly Dictionary<string, List<string>> _brokerSubscribedTopics = new(); // BrokerId -> List of TopicPatterns

        public MqttBackgroundService(
            ILogger<MqttBackgroundService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IMqttConnectionManager mqttConnectionManager)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _mqttConnectionManager = mqttConnectionManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MQTT Background Service is starting.");

            stoppingToken.Register(() => _logger.LogInformation("MQTT Background Service is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("MQTT Background Service running a check.");

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
                    var mqttOptionsBuilderService = scope.ServiceProvider.GetRequiredService<IMqttOptionsBuilderService>();
                    var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();

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
                        var (status, _) = await _mqttConnectionManager.GetConnectionStatusAsync(brokerId);
                        if (status != ConnectionStatus.Connected)
                        {
                            _logger.LogInformation("Broker '{BrokerId}' is not connected. Attempting to connect.", brokerId);
                            var options = await mqttOptionsBuilderService.BuildClientOptionsAsync(brokerId);
                            if (options != null)
                            {
                                await _mqttConnectionManager.ConnectAsync(brokerId, options);
                                (status, _) = await _mqttConnectionManager.GetConnectionStatusAsync(brokerId); // Re-check status
                            }
                        }

                        if (status == ConnectionStatus.Connected)
                        {
                            var client = await _mqttConnectionManager.GetClientAsync(brokerId);
                            if (client != null && !_activeMqttClients.ContainsKey(brokerId))
                            {
                                _activeMqttClients[brokerId] = client;
                                client.ApplicationMessageReceivedAsync += async e =>
                                {
                                    var payload = e.ApplicationMessage?.Payload == null ? string.Empty : Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                                    _logger.LogInformation("MQTT Message Received on topic {Topic} for broker {BrokerId}: {Payload}", e.ApplicationMessage.Topic, brokerId, payload.Replace("\n", "").Replace("\r", ""));

                                    // TODO: Process the received message (e.g., save to database, trigger events)
                                    await Task.CompletedTask;
                                };

                                client.DisconnectedAsync += async e =>
                                {
                                    _logger.LogWarning("MQTT client for broker '{BrokerId}' disconnected. Reason: {Reason}", brokerId, e.Reason);
                                    // Attempt to reconnect after a delay
                                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                                    _activeMqttClients.Remove(brokerId); // Remove from active clients to trigger re-connection logic in next loop
                                };
                            }

                            // Get all topics associated with this broker that are enabled and published
                            var topicContentItems = await contentManager.Query<ContentItem, ContentItemIndex>()
                                .Where(x => x.ContentType == "Topic" && x.Published)
                                .ListAsync();

                            var topicsToSubscribe = new List<MqttTopicFilter>();
                            var currentBrokerTopics = new List<string>();

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
                                        currentBrokerTopics.Add(topicPattern);

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

                            // Compare with currently subscribed topics for this broker
                            if (!_brokerSubscribedTopics.ContainsKey(brokerId) || !Enumerable.SequenceEqual(_brokerSubscribedTopics[brokerId].OrderBy(t => t), currentBrokerTopics.OrderBy(t => t)))
                            {
                                _logger.LogInformation("Updating subscriptions for broker '{BrokerId}'.", brokerId);

                                // Unsubscribe from old topics
                                if (_brokerSubscribedTopics.ContainsKey(brokerId))
                                {
                                    var topicsToUnsubscribe = _brokerSubscribedTopics[brokerId].Except(currentBrokerTopics).ToList();
                                    if (topicsToUnsubscribe.Any())
                                    {
                                        await client.UnsubscribeAsync(topicsToUnsubscribe.Select(t => new MqttTopicFilter { Topic = t }).ToList());
                                        _logger.LogInformation("Unsubscribed from topics for broker '{BrokerId}': {Topics}", brokerId, string.Join(", ", topicsToUnsubscribe));
                                    }
                                }

                                // Subscribe to new topics
                                if (topicsToSubscribe.Any())
                                {
                                    await client.SubscribeAsync(new MqttClientSubscribeOptions { TopicFilters = topicsToSubscribe });
                                    _logger.LogInformation("Subscribed to topics for broker '{BrokerId}': {Topics}", brokerId, string.Join(", ", topicsToSubscribe.Select(t => t.Topic)));
                                }

                                _brokerSubscribedTopics[brokerId] = currentBrokerTopics;
                            }
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Check every 30 seconds
            }

            _logger.LogInformation("MQTT Background Service has stopped.");
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MQTT Background Service is performing a graceful shutdown.");

            foreach (var clientEntry in _activeMqttClients)
            {
                var brokerId = clientEntry.Key;
                var client = clientEntry.Value;
                if (client.IsConnected)
                {
                    try
                    {
                        await client.DisconnectAsync(MqttClientDisconnectOptionsReason.NormalDisconnection, stoppingToken);
                        _logger.LogInformation("MQTT client for broker '{BrokerId}' disconnected during shutdown.", brokerId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disconnecting MQTT client for broker '{BrokerId}' during shutdown.", brokerId);
                    }
                }
                client.Dispose();
            }
            _activeMqttClients.Clear();
            _brokerSubscribedTopics.Clear();

            await base.StopAsync(stoppingToken);
        }
    }
}
