namespace AmazData.Module.Mqtt.Models;

public class MqttOptions
{
    public int ReconnectIntervalSeconds { get; set; } = 5;  // 重连间隔
    public byte DefaultQoS { get; set; } = 1;  // QoS 1: AtLeastOnce
    public int MaxReconnectAttempts { get; set; } = 10;  // 最大重试
}
