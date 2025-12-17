using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchardCore.BackgroundTasks;
using OrchardCore.Environment.Shell.Scope;
using AmazData.Module.Mqtt.Services;
using System.Diagnostics;
using YesSql;

namespace AmazData.Module.Mqtt.BackgroundServices
{
    /// <summary>
    /// MqttMessageProcessor 是一个 Orchard Core 后台任务，负责从 MqttMessageChannel 中异步读取 MQTT 消息，
    /// 并将其处理后持久化到数据库。
    /// 该任务以批处理方式运行，更符合 IBackgroundTask 的设计原则，有助于优化内存使用和系统稳定性。
    /// </summary>
    /// <remarks>
    /// 后台任务的 `[BackgroundTask]` 属性定义了其执行频率和其他特性。
    /// </remarks>
    // ... [BackgroundTask] 属性保持不变 ...
    public class MqttMessageProcessor : IBackgroundTask
    {
        /// <summary>
        /// 用于接收 MQTT 消息的通道。
        /// </summary>
        private readonly MqttMessageChannel _channel;
        /// <summary>
        /// 日志记录器，用于记录处理过程中的信息、调试信息和错误。
        /// </summary>
        private readonly ILogger<MqttMessageProcessor> _logger;

        private readonly ISession _session;

        /// <summary>
        /// 构造函数，通过依赖注入获取 MqttMessageChannel 和 ILogger 实例。
        /// </summary>
        /// <param name="channel">MQTT 消息通道。</param>
        /// <param name="logger">日志记录器。</param>
        public MqttMessageProcessor(
            MqttMessageChannel channel,
            ISession session,
            ILogger<MqttMessageProcessor> logger)
        {
            _channel = channel;
            _logger = logger;
            _session = session;
        }

        /// <summary>
        /// 执行后台任务的核心方法。
        /// 此方法采用批处理模式，在每次调度运行时，处理所有当前在通道中可用的消息。
        /// 这种方式避免了长时间阻塞任务，使其更适合作为 IBackgroundTask。
        /// </summary>
        /// <param name="serviceProvider">服务提供者，用于在运行时按需获取服务（例如 IBrokerService）。</param>
        /// <param name="cancellationToken">取消令牌，用于在任务被请求取消时停止处理。</param>
        public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            _logger.LogDebug("MqttMessageProcessor 批处理任务开始执行。");

            // 使用 while 循环和 _channel.TryRead 以批处理方式处理消息。
            // 每次 DoWorkAsync 调用会尽可能快地清空通道中当前所有消息，然后任务结束，
            // 等待 Orchard Core 后台任务调度器再次触发。
            // 这解决了之前 await foreach 可能导致任务长期阻塞和内存累积的问题。
            while (!cancellationToken.IsCancellationRequested && _channel.TryRead(out var message))
            {
                // 为每条消息创建一个新的、独立的 ShellScope。
                // 这确保了在消息处理过程中创建的所有服务和资源都可以在该消息处理完成后被及时释放，
                // 有效避免了内存泄露问题，提高了资源管理效率。
                await ShellScope.Current.UsingAsync(async scope =>
                {
                    var stopwatch = Stopwatch.StartNew(); // 启动计时器，记录单条消息处理耗时。
                    try
                    {
                        // 从当前 Scope 获取 IBrokerService 实例。
                        // 由于 Scope 是为每条消息创建的，所以每次获取的服务实例都是独立的，
                        // 且在 Scope 结束时会被正确处置。
                        var brokerService = scope.ServiceProvider.GetRequiredService<IBrokerService>();

                        // 调用服务将 MQTT 消息写入数据库。
                        await brokerService.CreateMessageRecordsAsync(
                            message.Topic,
                            message.Payload);

                        stopwatch.Stop(); // 停止计时。
                        _logger.LogInformation(
                            "消息处理成功。连接键: {ConnectionKey}, 主题: {Topic}, 耗时: {ElapsedMs} 毫秒",
                            message.ConnectionKey, message.Topic, stopwatch.Elapsed.TotalMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop(); // 即使处理失败，也要停止计时。
                        // 记录处理错误。将异常信息、消息的关键信息和处理耗时一并记录，方便排查问题。
                        // 这种处理方式确保单条消息的失败不会中断整个批处理任务。
                        _logger.LogError(
                            ex,
                            "处理 MQTT 消息时发生错误。连接键: {ConnectionKey}, 主题: {Topic}, 耗时: {ElapsedMs} 毫秒",
                            message.ConnectionKey, message.Topic, stopwatch.Elapsed.TotalMilliseconds);
                    }
                });
            }
            await _session.SaveChangesAsync();
            _logger.LogDebug("MqttMessageProcessor 批处理任务执行完毕。");
        }
    }
}