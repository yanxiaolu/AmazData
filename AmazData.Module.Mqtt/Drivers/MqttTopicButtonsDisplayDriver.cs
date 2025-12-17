using System.Linq;
using System.Threading.Tasks;
using AmazData.Module.Mqtt.Models;
using AmazData.Module.Mqtt.Services;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;

namespace AmazData.Module.Mqtt.Drivers
{
    public class MqttTopicButtonsDisplayDriver : ContentDisplayDriver
    {
        private readonly IContentManager _contentManager;
        private readonly IMqttSubscriptionManager _subscriptionManager;

        public MqttTopicButtonsDisplayDriver(IContentManager contentManager, IMqttSubscriptionManager subscriptionManager)
        {
            _contentManager = contentManager;
            _subscriptionManager = subscriptionManager;
        }

        public override async Task<IDisplayResult> DisplayAsync(ContentItem contentItem, BuildDisplayContext context)
        {
            if (context.DisplayType != "SummaryAdmin" || contentItem.ContentType != "Topic")
            {
                return null;
            }
            
            // We need the latest version to get the relationships.
            var latestContentItem = await _contentManager.GetAsync(contentItem.ContentItemId, VersionOptions.Latest);
            if (latestContentItem == null)
            {
                return null;
            }

            var topicPart = latestContentItem.As<TopicPart>();
            var topicPattern = topicPart?.TopicPattern?.Text;
            var brokerId = topicPart?.Broker?.ContentItemIds?.FirstOrDefault();

            var isConfigured = !string.IsNullOrEmpty(topicPattern) && !string.IsNullOrEmpty(brokerId);
            bool isSubscribed = false;

            if (isConfigured)
            {
                var subscriptions = await _subscriptionManager.ListSubscriptionsAsync(brokerId);
                isSubscribed = subscriptions.Contains(topicPattern);
            }

            return Initialize<MqttTopicButtonsViewModel>("MqttTopicButtons_Subscribe", model =>
                {
                    model.TopicId = contentItem.ContentItemId;
                    model.IsSubscribed = isSubscribed;
                    model.IsConfigured = isConfigured;
                })
                .Location("SummaryAdmin", "Actions:10");
        }
    }
}
