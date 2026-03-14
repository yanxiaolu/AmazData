using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using System.Collections.Concurrent;

namespace AmazData.Module.Mqtt.Services;

public class MqttConnectionManager : IMqttConnectionManager, IDisposable
{
    private static readonly Action<ILogger, string, Exception?> _logChannelWriteFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(ConnectAsync)),
            "[{Key}] Failed to write message to channel. Queue might be full.");

    private static readonly Action<ILogger, string, string, Exception?> _logEnqueuedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(2, nameof(ConnectAsync)),
            "[{Key}] Enqueued message from {Topic}");

    private static readonly Action<ILogger, string, string, Exception?> _logMqttMessageProcessingError =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(3, nameof(ConnectAsync)),
            "[{Key}] Error processing MQTT message. Topic: {Topic}");

    private static readonly Action<ILogger, string, string, Exception?> _logDisconnected =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(4, nameof(ConnectAsync)),
            "[{Key}] Disconnected: {Reason}");

    private static readonly Action<ILogger, string, Exception?> _logConnectionException =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(5, nameof(ConnectAsync)),
            "[{Key}] Connection exception");

    private static readonly Action<ILogger, string, string, Exception?> _logSubscribedTopic =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(6, nameof(SubscribeAsync)),
            "[{Key}] 已订阅主题: {Topic}");

    private static readonly Action<ILogger, string, string, Exception?> _logUnsubscribedTopic =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(7, nameof(UnsubscribeAsync)),
            "[{Key}] 已取消订阅主题: {Topic}");

    private static readonly Action<ILogger, string, string, Exception?> _logPublishedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(8, nameof(PublishAsync)),
            "[{Key}] 消息已发送 -> {Topic}");

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
    
    public bool IsConnected(string key)
    {
        return _clients.TryGetValue(key, out var client) && client.IsConnected;
    }

    public async Task<bool> ConnectAsync(BrokerConfig config)
    {
        // 检查是否已连接
        if (_clients.TryGetValue(config.Key, out var existingClient) && existingClient.IsConnected)
        {
            return true;
        }

        // 创建新客户端
        var client = _clientFactory.CreateMqttClient();
        _subscriptions.TryAdd(config.Key, new ConcurrentBag<string>());

        // 构建 Options
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(config.Host, config.Port)
            .WithClientId(string.IsNullOrEmpty(config.ClientId) ? Guid.NewGuid().ToString() : config.ClientId)
            .WithCleanSession()
            .WithTimeout(TimeSpan.FromSeconds(10));

        if (!string.IsNullOrEmpty(config.Username)) options.WithCredentials(config.Username, config.Password);

        // --- 事件处理 (已添加异常处理) ---

        client.ApplicationMessageReceivedAsync += async e =>
        {
            // 【核心修改】顶层 try-catch 包裹，防止回调中的异常导致应用崩溃
            try
            {
                var payloadStr = e.ApplicationMessage.ConvertPayloadToString();
                var eventArgs = new BrokerMessageEventArgs(config.Key, e.ApplicationMessage.Topic, payloadStr);

                // 1. 触发 C# 事件
                // 注意：如果订阅者逻辑中有未捕获异常，也会被外层 catch 捕获，保护 MQTT 连接
                OnMessageReceived?.Invoke(this, eventArgs);

                // 2. 写入 Channel
                if (!_messageChannel.TryWrite(eventArgs))
                {
                    _logChannelWriteFailed(_logger, config.Key, null);
                }

                _logEnqueuedMessage(_logger, config.Key, e.ApplicationMessage.Topic, null);
            }
            catch (Exception ex)
            {
                // 记录详细错误信息，包括出错的主题，方便排查
                _logMqttMessageProcessingError(
                    _logger,
                    config.Key,
                    e.ApplicationMessage?.Topic ?? "Unknown",
                    ex);
            }

            await Task.CompletedTask;
        };

        client.DisconnectedAsync += e =>
        {
            _logDisconnected(_logger, config.Key, e.Reason.ToString(), null);
            _subscriptions.TryRemove(config.Key, out _);
            return Task.CompletedTask;
        };

        // --- 连接逻辑 ---
        try
        {
            var result = await client.ConnectAsync(options.Build());

            if (result.ResultCode == MqttClientConnectResultCode.Success)
            {
                _clients.AddOrUpdate(config.Key, client, (k, old) => { old.Dispose(); return client; });
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logConnectionException(_logger, config.Key, ex);
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
        _logSubscribedTopic(_logger, key, topic, null);
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

        _logUnsubscribedTopic(_logger, key, topic, null);
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
        _logPublishedMessage(_logger, key, topic, null);
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
