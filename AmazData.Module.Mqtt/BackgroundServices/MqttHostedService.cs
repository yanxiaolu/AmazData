using System;
using System.Text;
using System.Threading;
using AmazData.Module.Mqtt.Models;
using AmazData.Module.Mqtt.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.BackgroundServices;

public class MqttHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttHostedService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30); // 每30秒检查一次，可配置

    public MqttHostedService(IServiceScopeFactory scopeFactory, ILogger<MqttHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 根据你依赖的版本，这里使用 MqttFactory 更常见、兼容性好
                var factory = new MqttClientFactory();
                var mqttClient = factory.CreateMqttClient();

                // 消息到达时的处理器
                mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    var payload = e.ApplicationMessage?.Payload == null ? string.Empty : Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    Console.WriteLine($"[{DateTime.Now:O}] Topic: {e.ApplicationMessage?.Topic} QoS: {e.ApplicationMessage?.QualityOfServiceLevel}");
                    Console.WriteLine($"Payload: {payload}");
                    Console.WriteLine(new string('-', 60));
                    return Task.CompletedTask;
                };

                // 连接/重连成功后订阅（放在这里能在自动重连后再次订阅）
                mqttClient.ConnectedAsync += async e =>
                {
                    Console.WriteLine("MQTT 已连接。订阅主题...");
                    await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("system/MonitorData").WithAtLeastOnceQoS().Build());
                    Console.WriteLine("订阅完成：system/MonitorData");
                };

                mqttClient.DisconnectedAsync += e =>
                {
                    Console.WriteLine($"MQTT 已断开（reason: {e.Reason}）。异常: {e.Exception?.Message}");
                    return Task.CompletedTask;
                };

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("8.152.96.245") // 替换为你的 broker
                                                   // 可选：.WithCleanSession(false) 来保持会话（有些 broker/用例）
                    .Build();

                await mqttClient.ConnectAsync(options, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT 服务检查异常。");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}
