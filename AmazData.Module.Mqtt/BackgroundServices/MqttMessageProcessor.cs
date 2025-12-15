using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchardCore.BackgroundTasks;
using OrchardCore.Environment.Shell.Scope;
using AmazData.Module.Mqtt.Services;
using System.Diagnostics; // 【新增】用于计时

namespace AmazData.Module.Mqtt.BackgroundServices
{
    // ... [BackgroundTask] 属性保持不变 ...
    public class MqttMessageProcessor : IBackgroundTask
    {
        private readonly MqttMessageChannel _channel;
        private readonly ILogger<MqttMessageProcessor> _logger;

        public MqttMessageProcessor(
            MqttMessageChannel channel,
            ILogger<MqttMessageProcessor> logger)
        {
            _channel = channel;
            _logger = logger;
        }

        public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            _logger.LogDebug("MqttMessageProcessor (IBackgroundTask) started reading from channel.");

            // 持续从 Channel 读取消息。
            await foreach (var message in _channel.ReadAllAsync(cancellationToken))
            {
                // 【新增】启动计时器，记录从 Channel 取出消息到写入数据库的总耗时
                var stopwatch = Stopwatch.StartNew(); 
                
                try
                {
                    // 【关键】使用 ShellScope.Current.UsingAsync 来创建一个隔离的、有 Tenant Context 的 Scope。
                    await ShellScope.Current.UsingAsync(async scope =>
                    {
                        var brokerService = scope.ServiceProvider.GetRequiredService<IBrokerService>();

                        // 调用 Service 写入数据库
                        await brokerService.CreateMessageRecordsAsync(
                            message.ConnectionKey,
                            message.Topic,
                            message.Payload);
                    });

                    // 【新增】停止计时并记录日志
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "Message processed successfully. ConnectionKey: {ConnectionKey}, Topic: {Topic}, Elapsed: {ElapsedMs} ms",
                        message.ConnectionKey, message.Topic, stopwatch.Elapsed.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop(); // 即使失败也要停止计时
                    // 记录错误，并记录耗时
                    _logger.LogError(
                        ex, 
                        "Error processing MQTT message in MqttMessageProcessor. ConnectionKey: {ConnectionKey}, Topic: {Topic}, Elapsed: {ElapsedMs} ms",
                        message.ConnectionKey, message.Topic, stopwatch.Elapsed.TotalMilliseconds);
                }

                // 检查取消请求
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("MqttMessageProcessor task received cancellation request.");
                    break;
                }
            }
        }
    }
}