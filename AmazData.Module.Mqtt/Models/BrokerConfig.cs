using System;

namespace AmazData.Module.Mqtt.Models;

// 连接配置：用于传递 Broker 信息
public record BrokerConfig(
    string Key,             // 唯一标识，如 "Aliyun", "Local"
    string Host,
    int Port = 1883,
    string ClientId = "",
    string? Username = null,
    string? Password = null
);

// 消息事件参数：包含来源 Broker 的 Key
public class BrokerMessageEventArgs : EventArgs
{
    public string ConnectionKey { get; }
    public string Topic { get; }
    public string Payload { get; }

    public BrokerMessageEventArgs(string key, string topic, string payload)
    {
        ConnectionKey = key;
        Topic = topic;
        Payload = payload;
    }
}