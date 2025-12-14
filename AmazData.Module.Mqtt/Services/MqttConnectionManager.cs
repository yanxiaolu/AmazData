using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using System.Collections.Concurrent;

namespace AmazData.Module.Mqtt.Services;

public class MqttConnectionManager : IMqttConnectionManager, IDisposable
{
    private readonly ConcurrentDictionary<string, IMqttClient> _clients = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _subscriptions = new();
    private readonly MqttClientFactory _clientFactory;
    private readonly ILogger<MqttConnectionManager> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    // 注入 Channel
    private readonly MqttMessageChannel _messageChannel;

    public event EventHandler<BrokerMessageEventArgs>? OnMessageReceived;

    public MqttConnectionManager(
        ILogger<MqttConnectionManager> logger,
        IServiceScopeFactory scopeFactory,
        MqttMessageChannel messageChannel) // 注入
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _messageChannel = messageChannel;
        _clientFactory = new MqttClientFactory();
    }

    public async Task<bool> ConnectAsync(BrokerConfig config)
    {
        if (_clients.TryGetValue(config.Key, out var existingClient) && existingClient.IsConnected)
        {
            return true;
        }

        var client = _clientFactory.CreateMqttClient();
        _subscriptions.TryAdd(config.Key, new ConcurrentBag<string>());

        // 构建 Options (省略部分代码，与原版一致，建议提取为 private helper method)
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(config.Host, config.Port)
            .WithClientId(string.IsNullOrEmpty(config.ClientId) ? Guid.NewGuid().ToString() : config.ClientId)
            .WithCleanSession()
            .WithTimeout(TimeSpan.FromSeconds(10));

        if (!string.IsNullOrEmpty(config.Username)) options.WithCredentials(config.Username, config.Password);

        // --- 事件处理 ---

        client.ApplicationMessageReceivedAsync += async e =>
        {
            var payloadStr = e.ApplicationMessage.ConvertPayloadToString();
            var eventArgs = new BrokerMessageEventArgs(config.Key, e.ApplicationMessage.Topic, payloadStr);

            // 1. 触发 C# 事件 (给实时 UI 或 SignalR 使用)
            OnMessageReceived?.Invoke(this, eventArgs);

            // 2. 【核心变化】写入 Channel，而不是直接写库
            // 这是一个极快的内存操作，不会阻塞 MQTT 线程
            if (!_messageChannel.TryWrite(eventArgs))
            {
                _logger.LogWarning("Failed to write message to channel. Queue might be full.");
            }

            _logger.LogDebug("[{Key}] Enqueued message from {Topic}", config.Key, e.ApplicationMessage.Topic);

            await Task.CompletedTask;
        };

        client.DisconnectedAsync += async e =>
        {
            _logger.LogWarning("[{Key}] Disconnected: {Reason}", config.Key, e.Reason);
            _subscriptions.TryRemove(config.Key, out _);
            if (e.ClientWasConnected)
            {
                // 状态更新频率低，可以直接在这里用 Scope
                await UpdateDbStateAsync(config.Key, false);
            }
        };

        try
        {
            var result = await client.ConnectAsync(options.Build());

            if (result.ResultCode == MqttClientConnectResultCode.Success)
            {
                _clients.AddOrUpdate(config.Key, client, (k, old) => { old.Dispose(); return client; });
                // 这里建议由 Controller 调用 UpdateDbStateAsync(true)，或者保留在这里
                // 如果保留在这里，确保 UpdateDbStateAsync 内部使用了 CommitAsync
                await UpdateDbStateAsync(config.Key, true);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Key}] Connection exception", config.Key);
            return false;
        }
    }

    public async Task SubscribeAsync(string key, string topic)
    {
        var client = GetClient(key);

        // [符合官方示例 Client_Subscribe_Samples.cs]
        // v5.0 必须构建 MqttClientSubscribeOptions
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(f =>
            {
                f.WithTopic(topic);
                f.WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
            })
            .Build();

        // 调用 SubscribeAsync 传入 Options
        var result = await client.SubscribeAsync(subscribeOptions);
        
        if (_subscriptions.TryGetValue(key, out var topics))
        {
            if (!topics.Contains(topic))
            {
                topics.Add(topic);
            }
        }
        
        // 可以在这里检查 result.Items 来确认每个 topic 的订阅结果
        _logger.LogInformation($"[{key}] 已订阅主题: {topic}");
    }
    
    public async Task UnsubscribeAsync(string key, string topic)
    {
        var client = GetClient(key);
        await client.UnsubscribeAsync(topic);

        if (_subscriptions.TryGetValue(key, out var topics))
        {
            // ConcurrentBag doesn't have a direct Remove. We need to recreate it.
            var newTopics = new ConcurrentBag<string>(topics.Except(new[] { topic }));
            _subscriptions.AddOrUpdate(key, newTopics, (k, old) => newTopics);
        }

        _logger.LogInformation($"[{key}] 已取消订阅主题: {topic}");
    }

    public Task<IReadOnlyList<string>> GetSubscriptionsAsync(string key)
    {
        if (_subscriptions.TryGetValue(key, out var topics))
        {
            return Task.FromResult<IReadOnlyList<string>>(topics.ToList());
        }

        return Task.FromResult<IReadOnlyList<string>>(new List<string>());
    }

    public async Task PublishAsync(string key, string topic, string payload)
    {
        var client = GetClient(key);

        // 构建消息
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await client.PublishAsync(message);
        _logger.LogDebug($"[{key}] 消息已发送 -> {topic}");
    }

    public async Task DisconnectAsync(string key)
    {
        if (_clients.TryRemove(key, out var client))
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection).Build());
            }
            _subscriptions.TryRemove(key, out _);
            await UpdateDbStateAsync(key, false);
            client.Dispose();
        }
    }

    private IMqttClient GetClient(string key)
    {
        if (_clients.TryGetValue(key, out var client) && client.IsConnected)
        {
            return client;
        }
        throw new InvalidOperationException($"客户端 [{key}] 未找到或未连接。请先调用 ConnectAsync。");
    }
    private async Task UpdateDbStateAsync(string brokerId, bool isConnected)
    {
        using var scope = _scopeFactory.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<IBrokerService>();
        // BrokerService 内部已经有了 CommitAsync，所以这里 await 即可
        await updater.UpdateConnectionStateAsync(brokerId, isConnected);
    }
    public void Dispose()
    {
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }
        _clients.Clear();
        _subscriptions.Clear();
    }
}