using System;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;

namespace AmazData.Module.Mqtt.Services;

public interface IMqttClientManager
{
    Task<IManagedMqttClient> GetManagedClientAsync(string brokerId);  // 获取Managed客户端（支持重连）
    Task SubscribeWithReconnectAsync(string brokerId, string topicPattern, byte qos, Func<MqttApplicationMessage, Task> onMessage);  // 订阅+重连，委托消息处理
    Task UnsubscribeAsync(string brokerId, string topicPattern);  // 退订
}
