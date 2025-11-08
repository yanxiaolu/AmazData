using System.Text;
using MQTTnet;

public static class Client_Connection_Samples
{
    public static async Task ConnectAndRunAsync(CancellationToken cancellationToken)
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

        await mqttClient.ConnectAsync(options, cancellationToken);

        // 主任务等待直到取消（例如按 Ctrl+C）
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // 取消时继续优雅断开
        }

        Console.WriteLine("正在断开连接...");
        await mqttClient.DisconnectAsync();
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        // 捕获 Ctrl+C 和 SIGTERM
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true; // 不让进程立即退出，让我们优雅关闭
            cts.Cancel();
        };

        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            cts.Cancel();
        };

        await Client_Connection_Samples.ConnectAndRunAsync(cts.Token);
        Console.WriteLine("程序已退出。");
    }
}