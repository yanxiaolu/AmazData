using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using System.Threading.Tasks;
using AmazData.Module.Mqtt.Services;
using AmazData.Module.Mqtt.Models;
using OrchardCore.DisplayManagement.Notify;
using Microsoft.AspNetCore.Mvc.Localization;

namespace AmazData.Module.Mqtt.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IMqttConnectionManager _mqttConnectionManager;
        private readonly IMqttSubscriptionManager _mqttSubscriptionManager;
        private readonly INotifier _notifier;
        private readonly IHtmlLocalizer<SubscriptionController> _localizer;

        public SubscriptionController(
            IContentManager contentManager,
            IMqttConnectionManager mqttConnectionManager,
            IMqttSubscriptionManager mqttSubscriptionManager,
            INotifier notifier,
            IHtmlLocalizer<SubscriptionController> localizer)
        {
            _contentManager = contentManager;
            _mqttConnectionManager = mqttConnectionManager;
            _mqttSubscriptionManager = mqttSubscriptionManager;
            _notifier = notifier;
            _localizer = localizer;
        }

        public async Task<IActionResult> Subscribe(string topicId)
        {
            var topicContentItem = await _contentManager.GetAsync(topicId);
            var topicPart = topicContentItem?.As<TopicPart>();
            if (topicPart == null)
            {
                return NotFound("Topic not found.");
            }

            var brokerContentItemIds = topicPart.Broker.ContentItemIds;
            if (brokerContentItemIds == null || brokerContentItemIds.Length == 0)
            {
                return BadRequest("Broker not associated with this topic.");
            }

            var brokerId = brokerContentItemIds[0];
            var (status, _) = await _mqttConnectionManager.GetConnectionStatusAsync(brokerId);

            if (status != ConnectionStatus.Connected)
            {
                await _notifier.WarningAsync(_localizer["Broker is not connected. Please connect the broker first."]);
                return RedirectToAction("List", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "Topic" });
            }

            await _mqttSubscriptionManager.SubscribeAsync(topicId);

            await _notifier.SuccessAsync(_localizer["Topic subscribed successfully."]);
            return RedirectToAction("List", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "Topic" });
        }
    }
}
