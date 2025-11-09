using MQTTnet;
using MQTTnet.Client.Options;

namespace AmazData.Module.Mqtt.Services
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    public interface IMqttConnectionManager
    {
        Task<bool> ConnectAsync(string connectionId, MqttClientOptions options);
        Task DisconnectAsync(string connectionId);
        Task<(ConnectionStatus Status, string? LastError)> GetConnectionStatusAsync(string connectionId);
        Task<IMqttClient?> GetClientAsync(string connectionId);
    }
}