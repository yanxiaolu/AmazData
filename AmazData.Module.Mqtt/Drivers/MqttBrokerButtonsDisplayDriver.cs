using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using AmazData.Module.Mqtt.Models;
using AmazData.Module.Mqtt.Services;

namespace AmazData.Module.Mqtt.Drivers;

public class MqttBrokerButtonsDisplayDriver : ContentDisplayDriver
{
    private readonly IMqttConnectionManager _mqttConnectionManager;

    public MqttBrokerButtonsDisplayDriver(IMqttConnectionManager mqttConnectionManager)
    {
        _mqttConnectionManager = mqttConnectionManager;
    }

    public override IDisplayResult Display(ContentItem contentItem, BuildDisplayContext context)
    {
        // 只在后台列表视图中显示
        if (context.DisplayType != "SummaryAdmin")
        {
            return null;
        }
        if (contentItem.ContentType != "Broker")
        {
            return null;
        }

        // 注册 Shape，传递 ContentItem 作为模型
        return Initialize<MqttBrokerButtonsViewModel>("MqttBrokerButtons_Start", model =>
            {
                model.ContentItem = contentItem;
                model.IsConnected = _mqttConnectionManager.IsConnected(contentItem.ContentItemId);
            })
            .Location("SummaryAdmin", "Actions:10");
    }
}