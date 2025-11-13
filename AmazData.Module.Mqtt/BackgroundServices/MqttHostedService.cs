using Microsoft.Extensions.Logging;
using OrchardCore.BackgroundTasks;

namespace AmazData.Module.Mqtt.BackgroundServices;

[BackgroundTask(Schedule = "*/1 * * * *")] // 每分钟轮询
public class MqttBackgroundTask : IBackgroundTask
{
    private readonly ILogger<MqttBackgroundTask> _logger;
    private int _executionCount = 0;
    public MqttBackgroundTask(ILogger<MqttBackgroundTask> logger)
    {
        _logger = logger;
    }

    public Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        _executionCount++;
        _logger.LogInformation("MqttBackgroundTask is running. Execution count: {Count}", _executionCount);
        return Task.CompletedTask;
    }
}