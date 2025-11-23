using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using System.Collections.Concurrent;
using System.Text;

namespace AmazData.Module.Mqtt.Services;

public class MqttConnectionManager : IMqttConnectionManager, IDisposable
{
    // 线程安全的字典，存储 {Key : Client}
    private readonly ConcurrentDictionary<string, IMqttClient> _clients = new();
    private readonly MqttClientFactory _clientFactory; // v5.0 核心工厂
    private readonly ILogger<MqttConnectionManager> _logger;

    public event EventHandler<BrokerMessageEventArgs>? OnMessageReceived;

    public MqttConnectionManager(ILogger<MqttConnectionManager> logger)
    {
        _logger = logger;
        // [符合官方示例] 实例化 MqttClientFactory
        _clientFactory = new MqttClientFactory();
    }

    public async Task<bool> ConnectAsync(BrokerConfig config)
    {
        // 如果已存在且已连接，直接返回
        if (_clients.TryGetValue(config.Key, out var existingClient) && existingClient.IsConnected)
        {
            _logger.LogWarning($"Broker [{config.Key}] 已经是连接状态。");
            return true;
        }

        // 1. 创建客户端 (使用 MqttClientFactory)
        var client = _clientFactory.CreateMqttClient();

        // 2. 构建连接选项 (使用 MqttClientOptionsBuilder)
        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(config.Host, config.Port)
            .WithClientId(string.IsNullOrEmpty(config.ClientId) ? Guid.NewGuid().ToString() : config.ClientId)
            // v5.0 建议使用 CleanSession (对于 MQTT 3.x) 或 CleanStart (对于 MQTT 5.0)
            .WithCleanSession()
            .WithTimeout(TimeSpan.FromSeconds(10));

        if (!string.IsNullOrEmpty(config.Username))
        {
            optionsBuilder.WithCredentials(config.Username, config.Password);
        }

        var options = optionsBuilder.Build();

        // 3. 挂载消息接收事件
        client.ApplicationMessageReceivedAsync += async e =>
        {
            // 获取 Payload
            // 注意：v5 中 Payload 是 ReadOnlyMemory<byte>
            // 直接使用 ConvertPayloadToString()，它会自动处理 Payload 为 null 的情况
            var payloadStr = e.ApplicationMessage.ConvertPayloadToString();
            // 触发外部事件
            OnMessageReceived?.Invoke(this, new BrokerMessageEventArgs(config.Key, e.ApplicationMessage.Topic, payloadStr));

            await Task.CompletedTask;
        };

        try
        {
            _logger.LogInformation($"[{config.Key}] 正在连接...");
            // 4. 执行连接
            var result = await client.ConnectAsync(options);

            if (result.ResultCode == MqttClientConnectResultCode.Success)
            {
                // 更新字典
                _clients.AddOrUpdate(config.Key, client, (k, old) =>
                {
                    old.Dispose(); // 如果有旧的，先销毁
                    return client;
                });
                _logger.LogInformation($"[{config.Key}] 连接成功!");
                return true;
            }
            else
            {
                _logger.LogError($"[{config.Key}] 连接失败: {result.ResultCode} - {result.ReasonString}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{config.Key}] 连接异常");
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

        // 可以在这里检查 result.Items 来确认每个 topic 的订阅结果
        _logger.LogInformation($"[{key}] 已订阅主题: {topic}");
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
                // [符合官方示例 Client_Connection_Samples.cs]
                // v5.0 必须构建 MqttClientDisconnectOptions
                var disconnectOptions = new MqttClientDisconnectOptionsBuilder()
                    .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                    .Build();

                await client.DisconnectAsync(disconnectOptions);
            }
            client.Dispose();
            _logger.LogInformation($"[{key}] 已断开并清理资源");
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
    }
}