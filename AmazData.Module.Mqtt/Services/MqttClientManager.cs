using System;
using System.Collections.Concurrent;
using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Services;

public class MqttClientManager : IMqttClientManager, IDisposable
{
    private readonly IContentManager _contentManager;
    private readonly IOptions<MqttOptions> _options;
    private readonly ILogger<MqttClientManager> _logger;
    private readonly ConcurrentDictionary<string, IManagedMqttClient> _clients = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _subscriptions = new();  // 跟踪订阅，幂等

    public MqttClientManager(IContentManager contentManager, IOptions<MqttOptions> options, ILogger<MqttClientManager> logger)
    {
        _contentManager = contentManager;
        _options = options;
        _logger = logger;
    }

    public async Task<IManagedMqttClient> GetManagedClientAsync(string brokerId)
    {
        if (_clients.TryGetValue(brokerId, out var client) && client.IsConnected)
        {
            return client;
        }

        var brokerItem = await _contentManager.GetAsync(brokerId);
        if (brokerItem == null)
        {
            throw new InvalidOperationException($"Broker with ID '{brokerId}' not found.");
        }

        var part = brokerItem.As<BrokerPart>();
        var factory = new MqttFactory();
        var managedClient = factory.CreateManagedMqttClient();

        var builder = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer(part.Host, part.Port)
                .WithClientId($"OrchardMqtt-{Guid.NewGuid()}")  // 唯一ID
                .WithCredentials(part.Username, part.Password)  // 生产：加密读取
                .WithCommunicationTimeout(TimeSpan.FromSeconds(10))
                .Build())
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(_options.Value.ReconnectIntervalSeconds))
            .WithMaximumReconnectAttempts(_options.Value.MaxReconnectAttempts);

        // 连接事件：重连后自动重新订阅（基于_samples的ConnectedAsync模式）
        managedClient.ConnectedAsync += async e =>
        {
            _logger.LogInformation($"Connected to Broker '{brokerId}'. Re-subscribing topics.");
            await ReSubscribeAllAsync(brokerId, e.ConnectResult);
            return Task.CompletedTask;
        };

        managedClient.DisconnectedAsync += async e =>
        {
            _logger.LogWarning($"Disconnected from Broker '{brokerId}'. Reason: {e.Reason}");
            return Task.CompletedTask;
        };

        await managedClient.StartAsync(builder.Build());
        _clients[brokerId] = managedClient;
        _subscriptions[brokerId] = new HashSet<string>();  // 初始化订阅跟踪

        return managedClient;
    }

    public Task SubscribeWithReconnectAsync(string brokerId, string topicPattern, byte qos, Func<MqttApplicationMessage, Task> onMessage)
    {
        throw new NotImplementedException();
    }

    public Task UnsubscribeAsync(string brokerId, string topicPattern)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        foreach (var client in _clients.Values)
        {
            client?.StopAsync().Wait();  // 优雅停止
        }
        _clients.Clear();
        _subscriptions.Clear();
    }
}
