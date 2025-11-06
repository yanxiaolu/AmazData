using MQTTnet;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AmazData.Module.Mqtt.Services
{
    public interface IMqttSubscriptionManager
    {
        Task SubscribeAsync(string topicItemId);  // 传入 Topic ContentItem ID，订阅（内部加载 BrokerRef 和 TopicPattern）
        Task UnsubscribeAsync(string topicItemId);  // 取消订阅
        Task<IReadOnlyList<string>> ListSubscriptionsAsync(string brokerItemId);  // 列出 Broker 的订阅主题
        Task<long> GetMessageStatsAsync(string brokerItemId);  // 获取 Broker 消息总数
    }
}