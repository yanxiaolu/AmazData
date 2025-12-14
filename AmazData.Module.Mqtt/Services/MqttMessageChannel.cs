using System.Threading.Channels;
using AmazData.Module.Mqtt.Models;

namespace AmazData.Module.Mqtt.Services;

public class MqttMessageChannel
{
    private readonly Channel<BrokerMessageEventArgs> _channel;

    public MqttMessageChannel()
    {
        // Unbounded 意味着可以无限缓冲（需注意内存），也可以设置为 Bounded(1000) 来背压
        _channel = Channel.CreateUnbounded<BrokerMessageEventArgs>();
    }

    public bool TryWrite(BrokerMessageEventArgs message)
    {
        return _channel.Writer.TryWrite(message);
    }

    public IAsyncEnumerable<BrokerMessageEventArgs> ReadAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}