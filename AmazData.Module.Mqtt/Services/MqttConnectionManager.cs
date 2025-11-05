using System;
using System.Collections.Concurrent;
using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Services;

public class MqttConnectionManager : IMqttConnectionManager, IDisposable
{
    private readonly IContentManager _contentManager;
    private readonly ILogger<MqttConnectionManager> _logger;
    private readonly ConcurrentDictionary<string, IManagedMqttClient> _clients = new();
    private readonly ConcurrentDictionary<string, string> _lastErrors = new();  // 错误存储

    public MqttConnectionManager(IContentManager contentManager, ILogger<MqttConnectionManager> logger)
    {
        _contentManager = contentManager;
        _logger = logger;
    }
    public async Task<bool> ConnectAsync(string brokerId)
    {
        if (_clients.TryGetValue(brokerId, out var client) && client.IsConnected) return true;

        try
        {
            var brokerItem = await _contentManager.GetAsync(brokerId);
            if (brokerItem == null) throw new InvalidOperationException($"Broker '{brokerId}' not found.");

            var part = brokerItem.As<BrokerPart>();
            var factory = new MqttFactory();
            client = factory.CreateManagedMqttClient();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(part.BrokerAddress.Text, int.Parse(part.Port.Text ?? "1883"))
                .WithClientId(part.ClientId.Text)
                .WithCredentials(part.Username.Text, part.Password.Text)
                .WithTls(new MqttClientOptionsBuilderTlsParameters { UseTls = part.UseSSL.Value })
                .Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(clientOptions)
                .Build();

            client.ConnectedHandler = async e =>
            {
                try
                {
                    _logger.LogInformation($"Connected to Broker '{brokerId}'.");
                    _lastErrors.TryRemove(brokerId, out _);
                    part.ConnectionState.Text = "Connected";
                    await _contentManager.UpdateAsync(brokerItem);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in ConnectedHandler for broker {brokerId}");
                }
            };

            client.DisconnectedHandler = async e =>
            {
                try
                {
                    _logger.LogWarning($"Disconnected from Broker '{brokerId}'. Reason: {e.ReasonCode}");
                    _lastErrors[brokerId] = e.ReasonCode.ToString();
                    part.ConnectionState.Text = "Disconnected";
                    await _contentManager.UpdateAsync(brokerItem);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in DisconnectedHandler for broker {brokerId}");
                }
            };

            await client.StartAsync(managedOptions);
            _clients[brokerId] = client;

            return true;
        }
        catch (Exception ex)
        {
            _lastErrors[brokerId] = ex.Message;
            _logger.LogError(ex, $"Connect failed for Broker '{brokerId}'.");
            return false;
        }
    }

    public async Task DisconnectAsync(string brokerId)
    {
        if (!_clients.TryRemove(brokerId, out var client)) return;

        await client.StopAsync();
        client.Dispose();
        _lastErrors.TryRemove(brokerId, out _);

        var brokerItem = await _contentManager.GetAsync(brokerId);
        if (brokerItem != null)
        {
            brokerItem.Get<TextField>("ConnectionState").Text = "Disconnected";
            await _contentManager.UpdateAsync(brokerItem);
        }
    }

    public async Task<(ConnectionStatus Status, string? LastError)> GetConnectionStatusAsync(string brokerId)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        foreach (var brokerId in _clients.Keys)
        {
            DisconnectAsync(brokerId).Wait();
        }
        _clients.Clear();
        _lastErrors.Clear();
    }
}
