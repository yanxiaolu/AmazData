using AmazData.Module.Mqtt.Models;

namespace AmazData.Module.Mqtt.Services;

public interface IMqttConnectionManager
{
    Task<bool> ConnectAsync(BrokerConfig config);
    Task DisconnectAsync(string key);
    bool IsConnected(string key);
    Task SubscribeAsync(string key, string topic);
    Task UnsubscribeAsync(string key, string topic);
    Task<IReadOnlyList<string>> GetSubscriptionsAsync(string key);
    Task PublishAsync(string key, string topic, string payload);

    // 统一的消息事件
    event EventHandler<BrokerMessageEventArgs> OnMessageReceived;
}