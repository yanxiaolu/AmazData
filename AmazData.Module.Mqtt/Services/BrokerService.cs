using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentFields.Fields; // 必须引用，用于 new TextField()
using AmazData.Module.Mqtt.Models;
using OrchardCore.Title.Models;
using YesSql;      // 引用 BrokerPart

namespace AmazData.Module.Mqtt.Services;

public class BrokerService : IBrokerService
{
    private readonly IContentManager _contentManager;
    private readonly ISession _session;
    private readonly ILogger<BrokerService> _logger;

    public BrokerService(
        IContentManager contentManager,
        ISession session,
        ILogger<BrokerService> logger)
    {
        _contentManager = contentManager;
        _session = session;
        _logger = logger;
    }

    public async Task CreateMessageRecordsAsync(string contentItemId, string topic, string payload)
    {
        try
        {
            var newMessage = await _contentManager.NewAsync("DataRecord");

            newMessage.Alter<TitlePart>(part =>
            {
                // 使用 Guid 防止标题冲突，或者根据需求自定义
                part.Title = $"{contentItemId}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}";
            });

            newMessage.Alter<DataRecordPart>(part =>
            {
                part.Timestamp = new DateTimeField { Value = DateTime.UtcNow };
                part.JsonDocument = new TextField { Text = payload };
            });

            await _contentManager.CreateAsync(newMessage);
            // 只有需要立即在前台显示时才Publish，如果是日志记录，Draft也是可以的，视需求而定。
            // 这里假设需要 Publish
            await _contentManager.PublishAsync(newMessage);

            // 【关键】强制提交事务，确保在后台 Scope 中数据落地
            await _session.SaveChangesAsync();

            _logger.LogDebug("Message recorded for Topic: {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create message record for {Topic}", topic);
            throw; // 抛出异常以便上层获知
        }
    }

    public async Task UpdateConnectionStateAsync(string contentItemId, bool isConnected)
    {
        var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.DraftRequired);

        if (contentItem == null)
        {
            _logger.LogWarning("Broker {Id} not found.", contentItemId);
            return;
        }

        contentItem.Alter<BrokerPart>(part =>
        {
            if (part.ConnectionState == null) part.ConnectionState = new BooleanField();
            part.ConnectionState.Value = isConnected;
        });

        await _contentManager.UpdateAsync(contentItem);
        // 状态改变必须 Publish 才能让前台看到
        await _contentManager.PublishAsync(contentItem);

        // 强制提交，确保 Manager 中的 Scope 能生效
        await _session.SaveChangesAsync();

        _logger.LogInformation("Broker {Id} state updated to: {State}", contentItemId, isConnected);
    }
}