using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using AmazData.Module.Mqtt.Services;
using AmazData.Module.Mqtt.Models;
using OrchardCore.DisplayManagement.Notify;
using Microsoft.AspNetCore.Mvc.Localization;

namespace AmazData.Module.Mqtt.Controllers
{
    public class MqttTopicController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IMqttSubscriptionManager _subscriptionManager;
        private readonly INotifier _notifier;
        private readonly IHtmlLocalizer<MqttTopicController> _localizer;

        public MqttTopicController(
            IContentManager contentManager,
            IMqttSubscriptionManager subscriptionManager,
            INotifier notifier,
            IHtmlLocalizer<MqttTopicController> localizer)
        {
            _contentManager = contentManager;
            _subscriptionManager = subscriptionManager;
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
            if (topicPart is null || string.IsNullOrEmpty(topicPart.TopicPattern.Text))
            {
                await _notifier.ErrorAsync(_localizer["Topic pattern is not defined."]);
                return RedirectToAction("List", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "Topic" });
            }

            await _subscriptionManager.SubscribeAsync(topicId, topicPart.TopicPattern.Text);

            await _notifier.SuccessAsync(_localizer["Topic subscribed successfully."]);
            return RedirectToAction("List", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "Topic" });
        }
        
        public async Task<IActionResult> Unsubscribe(string topicId)
        {
            await _subscriptionManager.UnsubscribeAsync(topicId);

            await _notifier.SuccessAsync(_localizer["Topic unsubscribed successfully."]);
            return RedirectToAction("List", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "Topic" });
        }
    }
}
