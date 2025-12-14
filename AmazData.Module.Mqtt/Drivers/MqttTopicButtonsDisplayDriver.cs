using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using AmazData.Module.Mqtt.Models;

namespace AmazData.Module.Mqtt.Drivers
{
    public class MqttTopicButtonsDisplayDriver : ContentDisplayDriver
    {
        public override IDisplayResult Display(ContentItem contentItem, BuildDisplayContext context)
        {
            if (context.DisplayType != "SummaryAdmin" || contentItem.ContentType != "Topic")
            {
                return null;
            }

            return Initialize<MqttBrokerButtonsViewModel>("MqttTopicButtons_Subscribe", model => model.ContentItem = contentItem)
                .Location("SummaryAdmin", "Actions:10");
        }
    }
}
