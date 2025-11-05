using System;

namespace AmazData.Module.Mqtt.Services;

public interface IMqttConnectionManager
{
    Task<bool> ConnectAsync(string brokerId);  // 连接，返回是否成功

    Task DisconnectAsync(string brokerId);  // 断开连接

    Task<(ConnectionStatus Status, string? LastError)> GetConnectionStatusAsync(string brokerId);  // 获取状态和错误
}

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error
}
