using System.Threading.Channels;
using AmazData.Module.Mqtt.Models;

namespace AmazData.Module.Mqtt.Services;

/// <summary>
/// MqttMessageChannel 类负责在 MQTT 消息生产者（例如 MQTT 客户端接收器）
/// 和消费者（例如后台消息处理器）之间提供一个异步消息队列。
/// 它封装了 .NET 的 System.Threading.Channels，实现了高效、线程安全的消息传递。
/// </summary>
public class MqttMessageChannel
{
    /// <summary>
    /// 内部使用的 Channel 实例，用于存储 BrokerMessageEventArgs 类型的 MQTT 消息事件。
    /// </summary>
    private readonly Channel<BrokerMessageEventArgs> _channel;

    /// <summary>
    /// 构造函数，初始化一个无界（Unbounded）的 Channel。
    /// 无界 Channel 意味着它可以存储无限数量的消息，直到系统内存耗尽。
    /// 这需要生产者和消费者之间有合理的速率匹配，否则可能导致内存持续增长。
    /// 作为替代方案，可以考虑使用 Bounded(capacity) Channel 来实现背压（backpressure），
    /// 限制队列大小，防止内存溢出。
    /// </summary>
    public MqttMessageChannel()
    {
        _channel = Channel.CreateUnbounded<BrokerMessageEventArgs>();
    }

    /// <summary>
    /// 尝试将一条 MQTT 消息事件写入 Channel。
    /// 这是一个非阻塞操作。对于无界 Channel，此方法总是返回 true。
    /// </summary>
    /// <param name="message">要写入的 BrokerMessageEventArgs 消息。</param>
    /// <returns>如果写入成功则返回 true，否则返回 false（在有界 Channel 中队列满时可能发生）。</returns>
    public bool TryWrite(BrokerMessageEventArgs message)
    {
        return _channel.Writer.TryWrite(message);
    }

    /// <summary>
    /// 异步读取 Channel 中的所有消息。
    /// 返回一个 IAsyncEnumerable，允许消费者使用 await foreach 异步迭代消息，
    /// 直到 Channel 关闭或取消令牌被触发。
    /// </summary>
    /// <param name="ct">取消令牌，用于在任务取消时停止读取。</param>
    /// <returns>一个异步可枚举的 MQTT 消息事件序列。</returns>
    public IAsyncEnumerable<BrokerMessageEventArgs> ReadAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }

    /// <summary>
    /// 尝试从 Channel 中非阻塞地读取一条 MQTT 消息事件。
    /// 如果队列中有消息，则立即读取并返回 true；否则返回 false。
    /// 此方法用于实现批处理（batch processing）模式，以便在后台任务中一次性处理所有可用消息。
    /// </summary>
    /// <param name="message">如果读取成功，则包含读取到的 BrokerMessageEventArgs 消息。</param>
    /// <returns>如果成功读取到消息则返回 true，否则返回 false。</returns>
    public bool TryRead(out BrokerMessageEventArgs message)
    {
        return _channel.Reader.TryRead(out message);
    }
}