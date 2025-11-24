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
        private readonly INotifier _notifier;
        private readonly IHtmlLocalizer<SubscriptionController> _localizer;

        public SubscriptionController(
            IContentManager contentManager,
            IMqttConnectionManager mqttConnectionManager,
            INotifier notifier,
            IHtmlLocalizer<SubscriptionController> localizer)
        {
            _contentManager = contentManager;
            _mqttConnectionManager = mqttConnectionManager;
            _notifier = notifier;
            _localizer = localizer;
        }

        public async Task<IActionResult> Subscribe(string topicId)
        {
            var contentItem = await _contentManager.GetAsync(topicId);
            if (contentItem is null)
            {
                return NotFound("Topic not found.");
            }
            var topicPart = contentItem?.As<TopicPart>();


            var brokerContentItemIds = topicPart.Broker.ContentItemIds;
            if (brokerContentItemIds == null || brokerContentItemIds.Length == 0)
            {
                return BadRequest("Broker not associated with this topic.");
            }

            var brokerId = brokerContentItemIds[0];

            // var (status, _) = await _mqttConnectionManager.GetConnectionStatusAsync(brokerId);

            // if (status != ConnectionStatus.Connected)
            // {
            //     await _notifier.WarningAsync(_localizer["Broker is not connected. Please connect the broker first."]);
            //     return RedirectToAction("List", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "Topic" });
            // }

            await _mqttConnectionManager.SubscribeAsync(brokerId, topicPart.TopicPattern.Text);

            await _notifier.SuccessAsync(_localizer["Topic subscribed successfully."]);
            return RedirectToAction("List", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "Topic" });
        }
    }
}
