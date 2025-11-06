
using Microsoft.Extensions.Logging;
using MQTTnet;
using System.Collections.Concurrent;

namespace AmazData.Module.Mqtt.Services
{
    public class MqttConnectionManager : IMqttConnectionManager, IDisposable
    {
        private readonly ILogger<MqttConnectionManager> _logger;
        private readonly ConcurrentDictionary<string, IMqttClient> _clients = new();
        private readonly ConcurrentDictionary<string, ConnectionStatus> _statuses = new();
        private readonly ConcurrentDictionary<string, string?> _lastErrors = new();
        private readonly MqttClientFactory _mqttClientFactory;

        public MqttConnectionManager(ILogger<MqttConnectionManager> logger)
        {
            _logger = logger;
            _mqttClientFactory = new MqttClientFactory();
        }

        public async Task<bool> ConnectAsync(string connectionId, MqttClientOptions options)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            // If a client with this ID already exists, disconnect and remove it first.
            if (_clients.ContainsKey(connectionId))
            {
                await DisconnectAsync(connectionId);
            }

            var mqttClient = _mqttClientFactory.CreateMqttClient();
            _clients[connectionId] = mqttClient;
            _statuses[connectionId] = ConnectionStatus.Connecting;
            _lastErrors.TryRemove(connectionId, out _);

            mqttClient.ConnectedAsync += e =>
            {
                _statuses[connectionId] = ConnectionStatus.Connected;
                _logger.LogInformation("MQTT client '{ConnectionId}' connected.", connectionId);
                return Task.CompletedTask;
            };

            mqttClient.DisconnectedAsync += e =>
            {
                // If it was a clean disconnect, we just mark it as disconnected.
                // Otherwise, we mark it as an error state.
                if (e.Reason == MqttClientDisconnectReason.NormalDisconnection)
                {
                    _statuses[connectionId] = ConnectionStatus.Disconnected;
                    _logger.LogInformation("MQTT client '{ConnectionId}' disconnected cleanly.", connectionId);
                }
                else
                {
                    _statuses[connectionId] = ConnectionStatus.Error;
                    var errorMessage = e.Exception?.Message ?? "Unknown disconnection reason";
                    _lastErrors[connectionId] = errorMessage;
                    _logger.LogWarning(e.Exception, "MQTT client '{ConnectionId}' disconnected with error: {Error}", connectionId, errorMessage);
                }
                return Task.CompletedTask;
            };

            try
            {
                var connectResult = await mqttClient.ConnectAsync(options, CancellationToken.None);

                if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
                {
                    _logger.LogInformation("Successfully initiated connection for MQTT client '{ConnectionId}'.", connectionId);
                    return true;
                }
                else
                {
                    var errorMessage = $"Failed to connect MQTT client '{connectionId}': {connectResult.ReasonString}";
                    _logger.LogError(errorMessage);
                    _statuses[connectionId] = ConnectionStatus.Error;
                    _lastErrors[connectionId] = errorMessage;
                    // Clean up the failed client
                    _clients.TryRemove(connectionId, out _);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during connection for MQTT client '{ConnectionId}'.", connectionId);
                _statuses[connectionId] = ConnectionStatus.Error;
                _lastErrors[connectionId] = ex.Message;
                // Clean up the failed client
                _clients.TryRemove(connectionId, out _);
                return false;
            }
        }

        public async Task DisconnectAsync(string connectionId)
        {
            if (_clients.TryRemove(connectionId, out var client))
            {
                if (client.IsConnected)
                {
                    try
                    {
                        await client.DisconnectAsync(new MqttClientDisconnectOptions { Reason = MqttClientDisconnectOptionsReason.NormalDisconnection }, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while disconnecting MQTT client '{ConnectionId}'.", connectionId);
                    }
                }
                client.Dispose();
            }
            _statuses.TryRemove(connectionId, out _);
            _lastErrors.TryRemove(connectionId, out _);
        }

        public Task<(ConnectionStatus Status, string? LastError)> GetConnectionStatusAsync(string connectionId)
        {
            if (!_statuses.TryGetValue(connectionId, out var status))
            {
                return Task.FromResult((ConnectionStatus.Disconnected, (string?)null));
            }

            _lastErrors.TryGetValue(connectionId, out var error);
            return Task.FromResult((status, error));
        }

        public void Dispose()
        {
            var connectionIds = _clients.Keys.ToList();
            foreach (var connectionId in connectionIds)
            {
                DisconnectAsync(connectionId).Wait();
            }
            _clients.Clear();
            _statuses.Clear();
            _lastErrors.Clear();
        }
    }
}
