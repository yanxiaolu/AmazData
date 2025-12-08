using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentFields.Fields; // 必须引用，用于 new TextField()
using AmazData.Module.Mqtt.Models;
using OrchardCore.Title.Models;
using YesSql;      // 引用 BrokerPart

namespace AmazData.Module.Mqtt.Services
{
    public class BrokerService : IBrokerService
    {
        private readonly IContentManager _contentManager;
        private readonly ILogger<BrokerService> _logger;
        private readonly ISession _session;

        public BrokerService(IContentManager contentManager, ILogger<BrokerService> logger, ISession session)
        {
            _contentManager = contentManager;
            _logger = logger;
            _session = session;
        }
        public async Task CreateMessageRecordsAsync(string contentItemId, string topic, string payload)
        {
            // Implementation for creating message records goes here.
            // This is a placeholder to satisfy the interface requirement.
            var newMessage = await _contentManager.NewAsync("DataRecord");
            newMessage.Alter<TitlePart>(part =>
            {
                part.Title = contentItemId;
            });
            newMessage.Alter<DataRecordPart>(part =>
            {
                part.Timestamp = new DateTimeField { Value = DateTime.UtcNow };
                part.JsonDocument = new TextField { Text = payload };
            });
            await _contentManager.CreateAsync(newMessage);
            await _contentManager.PublishAsync(newMessage);
            // 在手动 Scope 或后台任务中，必须手动保存更改，否则数据会丢失。
            await _session.SaveChangesAsync();
            _logger.LogInformation($"Created message record for Broker '{contentItemId}' on topic '{topic}'.");
            await Task.CompletedTask;
        }
        public async Task UpdateConnectionStateAsync(string contentItemId, bool isConnected)
        {
            // 1. Always get a draft version to modify. This is crucial for versionable content.
            var contentItem = await _contentManager.GetAsync(contentItemId);

            if (contentItem == null)
            {
                _logger.LogWarning($"Content item with ID '{contentItemId}' not found. Could not update connection state.");
                return;
            }

            // 2. Use Alter<T> to modify the part.
            contentItem.Alter<BrokerPart>(part =>
            {
                // To robustly ensure change detection, we replace the entire field object 
                // instead of just modifying a property on the existing one.
                part.ConnectionState = new BooleanField { Value = isConnected };
            });

            // 3. Save the draft with the changes.
            await _contentManager.UpdateAsync(contentItem);

            // 4. Publish the draft to make the changes live.
            await _contentManager.PublishAsync(contentItem);
            await _session.SaveChangesAsync();
            _logger.LogInformation($"Broker connection state for item '{contentItemId}' updated to: {isConnected}");
        }
    }
}