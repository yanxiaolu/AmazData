using System;
using MQTTnet;

namespace AmazData.Module.Mqtt.Services;

public interface IMqttSubscriptionManager
{
    Task SubscribeAsync(string brokerId, string topicPattern, byte qos, Func<MqttApplicationMessage, Task> onMessage);  // 订阅（简化，无Reconnect，由ConnectionManager处理重连）

    Task UnsubscribeAsync(string brokerId, string topicPattern);  // 退订

    Task<IReadOnlyList<string>> ListSubscriptionsAsync(string brokerId);  // 订阅列表

    Task<long> GetMessageStatsAsync(string brokerId);  // 获取该Broker接收消息总数（可扩展为字典<topic, count>）
}
