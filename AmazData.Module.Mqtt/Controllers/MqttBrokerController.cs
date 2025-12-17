using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using AmazData.Module.Mqtt.Services;
using OrchardCore.DisplayManagement.Notify;
using Microsoft.AspNetCore.Mvc.Localization;
using AmazData.Module.Mqtt.Models;
using OrchardCore.Admin;

namespace AmazData.Module.Mqtt.Controllers
{
    public class MqttBrokerController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IMqttConnectionManager _mqttConnectionManager;
        private readonly INotifier _notifier;
        private readonly IHtmlLocalizer<MqttBrokerController> _localizer;
        private readonly IBrokerService _brokerService;
        public MqttBrokerController(
    IContentManager contentManager,
    IMqttConnectionManager mqttConnectionManager,
    INotifier notifier,
    IHtmlLocalizer<MqttBrokerController> localizer,
    IBrokerService brokerService
    )
        {
            _contentManager = contentManager;
            _mqttConnectionManager = mqttConnectionManager;
            _notifier = notifier;
            _localizer = localizer;
            _brokerService = brokerService;
        }
        [Admin]
        public async Task<IActionResult> ConnectBroker(string brokerId)
        {
            var contentItem = await _contentManager.GetAsync(brokerId);

            if (contentItem is null)
            {
                return NotFound();
            }
            var brokerPart = contentItem.As<BrokerPart>();
            //todo:Controller 瘦身 (Thin Controllers)
            //*问题: 在 MqttBrokerController 的 ConnectBroker 方法中，包含了根据 BrokerPart 构建 BrokerConfig 对象的逻辑。当这个配置逻辑变复杂时，Controller 就会变得臃肿。Controller
            //的职责应该是协调调度，而不是处理业务对象的构建。
            //*建议: 将创建 BrokerConfig 的逻辑封装起来。可以考虑在 BrokerPart.cs 中增加一个扩展方法或一个普通方法。：
            if (brokerPart is not null)
            {
                BrokerConfig brokerConfig = new BrokerConfig(
                    Key: brokerId,
                    Host: brokerPart.BrokerAddress.Text,
                    Port: int.TryParse(brokerPart.Port.Text, out var port) ? port : 1883,
                    ClientId: $"AmazData-{System.Environment.MachineName}-{brokerId}",
                    Username: brokerPart.Username.Text,
                    Password: brokerPart.Password.Text,
                    UseSSL: brokerPart.UseSSL.Value
                );
                await _mqttConnectionManager.ConnectAsync(brokerConfig);
                await _notifier.SuccessAsync(_localizer["Broker Connected successfully."]);
                return RedirectToAction("List", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "Broker" });
            }

            return NotFound();

        }
        [Admin]
        public async Task<IActionResult> DisconnectBroker(string brokerId)
        {
            if (string.IsNullOrEmpty(brokerId))
            {
                return NotFound();
            }

            await _mqttConnectionManager.DisconnectAsync(brokerId);
            await _notifier.SuccessAsync(_localizer["Broker disconnected successfully."]);
            return RedirectToAction("List", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "Broker" });
        }

    }
}
