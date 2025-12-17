namespace AmazData.Module.Mqtt.Services
{
    public interface IMqttSubscriptionManager
    {
        Task SubscribeAsync(string topicItemId, string topicToSubscribe);  // 传入 Topic ContentItem ID 和要订阅的主题
        Task UnsubscribeAsync(string topicItemId);  // 取消订阅
        Task<IReadOnlyList<string>> ListSubscriptionsAsync(string brokerItemId);  // 列出 Broker 的订阅主题
    }
}