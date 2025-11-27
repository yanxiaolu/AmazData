using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentFields.Fields; // 必须引用，用于 new TextField()
using AmazData.Module.Mqtt.Models;      // 引用 BrokerPart

namespace AmazData.Module.Mqtt.Services
{
    public class BrokerService : IBrokerService
    {
        private readonly IContentManager _contentManager;
        private readonly ILogger<BrokerService> _logger;

        public BrokerService(IContentManager contentManager, ILogger<BrokerService> logger)
        {
            _contentManager = contentManager;
            _logger = logger;
        }

        public async Task UpdateConnectionStateAsync(string contentItemId, bool isConnected)
        {
            // 1. 获取最新的草稿版本 (DraftRequired)
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.DraftRequired);

            if (contentItem == null) return;

            // 2. 使用 Alter<TPart> 修改内容
            // Alter 会自动执行: var part = item.As<T>(); action(part); item.Apply(part);
            contentItem.Alter<BrokerPart>(part =>
            {
                // 定义状态值
                var statusValue = isConnected ? 1 : 0;

                // 检查字段是否为空（如果是新建的内容项，字段对象可能尚未实例化）
                if (part.ConnectionState.Values == null)
                {
                    part.ConnectionState.Values = new string[] { };
                }

                // 对于 MultiTextField，Values 属性存储选中的值（一个字符串数组）
                // 由于 ConnectionState 是单选，我们将其设置为包含单个值的数组。
                part.ConnectionState.Values = new[] { part.ConnectionState.Values[statusValue] };
            });

            // 3. 更新并发布
            // UpdateAsync 会验证模型并更新数据库记录
            await _contentManager.UpdateAsync(contentItem);

            // 如果需要立即生效到前台，执行 Publish
            await _contentManager.PublishAsync(contentItem);
        }
    }
}