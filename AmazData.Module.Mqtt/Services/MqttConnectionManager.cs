using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.Logging;
using MQTTnet;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;
using System.Collections.Concurrent;
using System.Text;

namespace AmazData.Module.Mqtt.Services
{
    public class MqttConnectionManager : IMqttConnectionManager, IDisposable
    {
        private readonly ILogger<MqttConnectionManager> _logger;
        private readonly ConcurrentDictionary<string, IMqttClient> _clients = new();
        private readonly ConcurrentDictionary<string, ConnectionStatus> _statuses = new();
        private readonly ConcurrentDictionary<string, string?> _lastErrors = new();
        // 这个字段是关键，用来持久化记录每个连接需要订阅的主题
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _clientSubscribedTopics = new();
        private readonly MqttClientFactory _mqttClientFactory; // 使用 MqttFactory
        private readonly IContentManager _contentManager;

        public MqttConnectionManager(IContentManager contentManager, ILogger<MqttConnectionManager> logger)
        {
            _contentManager = contentManager;
            _logger = logger;
            _mqttClientFactory = new MqttClientFactory(); // 使用 MqttFactory
        }

        public Task<IMqttClient?> GetClientAsync(string connectionId)
        {
            _clients.TryGetValue(connectionId, out var client);
            return Task.FromResult(client);
        }

        public async Task<bool> ConnectAsync(string connectionId, MqttClientOptions options)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (_clients.ContainsKey(connectionId))
            {
                await DisconnectAsync(connectionId);
            }

            var mqttClient = _mqttClientFactory.CreateMqttClient();
            _clients[connectionId] = mqttClient;
            _statuses[connectionId] = ConnectionStatus.Connecting;
            _lastErrors.TryRemove(connectionId, out _);
            // 确保为新的连接初始化一个空的订阅列表
            _clientSubscribedTopics.TryAdd(connectionId, new ConcurrentBag<string>());

            // ✅ **修改点 1: 在 ConnectedAsync 中恢复订阅**
            mqttClient.ConnectedAsync += async e =>
            {
                _statuses[connectionId] = ConnectionStatus.Connected;
                _logger.LogInformation("MQTT client '{ConnectionId}' connected. Restoring subscriptions...", connectionId);

                if (_clientSubscribedTopics.TryGetValue(connectionId, out var topicsToSubscribe) && !topicsToSubscribe.IsEmpty)
                {
                    var topicFilters = topicsToSubscribe.Select(topic => new MqttTopicFilterBuilder().WithTopic(topic).Build()).ToList();
                    if (topicFilters.Any())
                    {
                        await mqttClient.SubscribeAsync(new MqttClientSubscribeOptions { TopicFilters = topicFilters });
                        _logger.LogInformation("Restored {Count} subscriptions for '{ConnectionId}'.", topicFilters.Count, connectionId);
                    }
                }
            };

            // ✅ **修改点 2: 在 ApplicationMessageReceivedAsync 中添加详细日志**
            mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var payload = e.ApplicationMessage.Payload.ToString() == null ? string.Empty : Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                var sanitizedPayload = payload.Replace("\n", " ").Replace("\r", " ");
                _logger.LogInformation(
                    "Message Received on client '{ConnectionId}': [Topic: {Topic}] [QoS: {QoS}] [Payload: {Payload}]",
                    connectionId,
                    e.ApplicationMessage?.Topic,
                    e.ApplicationMessage?.QualityOfServiceLevel,
                    sanitizedPayload);

                // Create a new scope to resolve scoped services
                var contentItem = await _contentManager.NewAsync("DataRecord");

                contentItem.Alter<DataRecordPart>(part =>
                {
                    part.Timestamp = new DateTimeField { Value = DateTime.UtcNow };
                    part.JsonDocument = new TextField { Text = sanitizedPayload };
                });

                await _contentManager.PublishAsync(contentItem);

                _logger.LogInformation("Created MqttDataRecord content item for message from topic {Topic}", e.ApplicationMessage.Topic);
            };

            mqttClient.DisconnectedAsync += async e =>
            {
                if (e.Reason == MqttClientDisconnectReason.NormalDisconnection)
                {
                    _statuses[connectionId] = ConnectionStatus.Disconnected;
                    _logger.LogInformation("MQTT client '{ConnectionId}' disconnected cleanly.", connectionId);
                }
                else
                {
                    _statuses[connectionId] = ConnectionStatus.Error;
                    var errorMessage = e.Exception?.Message ?? e.ReasonString;
                    _lastErrors[connectionId] = errorMessage;
                    _logger.LogWarning(e.Exception, "MQTT client '{ConnectionId}' disconnected with error: {Error}", connectionId, errorMessage);
                }
                await Task.CompletedTask;
            };

            try
            {
                // ... (ConnectAsync 的调用和后续逻辑不变)
                var connectResult = await mqttClient.ConnectAsync(options, CancellationToken.None);
                if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
                {
                    _logger.LogInformation("Successfully initiated connection for MQTT client '{ConnectionId}'.", connectionId);
                    return true;
                }
                else
                {
                    var errorMessage = $"Failed to connect MQTT client '{connectionId}': {connectResult.ReasonString}";
                    _logger.LogError(errorMessage);
                    _statuses[connectionId] = ConnectionStatus.Error;
                    _lastErrors[connectionId] = errorMessage;
                    _clients.TryRemove(connectionId, out _);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during connection for MQTT client '{ConnectionId}'.", connectionId);
                _statuses[connectionId] = ConnectionStatus.Error;
                _lastErrors[connectionId] = ex.Message;
                _clients.TryRemove(connectionId, out _);
                return false;
            }
        }

        // ✅ **修改点 3: 新增 AddSubscriptionAsync 方法**
        public async Task AddSubscriptionAsync(string connectionId, string topic)
        {
            if (!_clientSubscribedTopics.TryGetValue(connectionId, out var topics))
            {
                _logger.LogWarning("Attempted to add subscription to non-existent connection '{ConnectionId}'.", connectionId);
                return;
            }

            if (!topics.Contains(topic))
            {
                topics.Add(topic);
            }

            if (_clients.TryGetValue(connectionId, out var client) && client.IsConnected)
            {
                _logger.LogInformation("Dynamically subscribing client '{ConnectionId}' to topic '{Topic}'.", connectionId, topic);
                await client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
            }
        }

        public async Task DisconnectAsync(string connectionId)
        {
            if (_clients.TryRemove(connectionId, out var client))
            {
                if (client.IsConnected)
                {
                    try
                    {
                        await client.DisconnectAsync(MqttClientDisconnectOptionsReason.NormalDisconnection, "Normal disconnection");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while disconnecting MQTT client '{ConnectionId}'.", connectionId);
                    }
                }
                client.Dispose();
            }
            _statuses.TryRemove(connectionId, out _);
            _lastErrors.TryRemove(connectionId, out _);
        }

        public Task<(ConnectionStatus Status, string? LastError)> GetConnectionStatusAsync(string connectionId)
        {
            if (!_statuses.TryGetValue(connectionId, out var status))
            {
                return Task.FromResult((ConnectionStatus.Disconnected, (string?)null));
            }

            _lastErrors.TryGetValue(connectionId, out var error);
            return Task.FromResult((status, error));
        }

        public void Dispose()
        {
            var connectionIds = _clients.Keys.ToList();
            foreach (var connectionId in connectionIds)
            {
                DisconnectAsync(connectionId).GetAwaiter().GetResult();
            }
            _clients.Clear();
            _statuses.Clear();
            _lastErrors.Clear();
        }
    }
}