using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchardCore.BackgroundTasks;
using OrchardCore.Environment.Shell.Scope;
using AmazData.Module.Mqtt.Services;

namespace AmazData.Module.Mqtt.BackgroundServices
{
    // 使用 [BackgroundTask] 属性定义任务调度和描述。
    // 对于持续处理消息队列的任务，我们设置一个短间隔的调度（例如每分钟）
    // 但实际运行时，DoWorkAsync 中的 await foreach 循环会使其持续运行，直到被取消。
    [BackgroundTask(
        Schedule = "*/1 * * * *", // 每分钟检查一次，但由于是 Channel reader，一旦启动将持续运行
        Description = "Processes incoming MQTT messages from the internal channel and writes them to the database."
    )]
    public class MqttMessageProcessor : IBackgroundTask
    {
        private readonly MqttMessageChannel _channel;
        private readonly ILogger<MqttMessageProcessor> _logger;

        public MqttMessageProcessor(
            MqttMessageChannel channel,
            ILogger<MqttMessageProcessor> logger)
        {
            // MqttMessageChannel 是 Singleton，安全注入。
            _channel = channel;
            _logger = logger;
        }

        /// <summary>
        /// OrchardCore 后台任务执行方法。
        /// </summary>
        public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            _logger.LogDebug("MqttMessageProcessor (IBackgroundTask) started reading from channel.");

            // 持续从 Channel 读取消息。
            await foreach (var message in _channel.ReadAllAsync(cancellationToken))
            {
                try
                {
                    // 【关键】使用 ShellScope.Current.UsingAsync 来创建一个隔离的、有 Tenant Context 的 Scope。
                    // 这对于在后台任务中安全地访问 Scoped 服务（如 IBrokerService 和 YesSql ISession）至关重要。
                    await ShellScope.Current.UsingAsync(async scope =>
                    {
                        var brokerService = scope.ServiceProvider.GetRequiredService<IBrokerService>();

                        // 调用 Service 写入数据库
                        await brokerService.CreateMessageRecordsAsync(
                            message.ConnectionKey,
                            message.Topic,
                            message.Payload);
                    });
                }
                catch (Exception ex)
                {
                    // 记录错误，不抛出，以防止中断整个后台任务循环。
                    _logger.LogError(ex, "Error processing MQTT message in MqttMessageProcessor. ConnectionKey: {ConnectionKey}, Topic: {Topic}",
                        message.ConnectionKey, message.Topic);
                }

                // 检查取消请求，以响应 Orchard Core 的停止任务信号
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("MqttMessageProcessor task received cancellation request.");
                    break;
                }
            }
        }
    }
}