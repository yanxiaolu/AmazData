using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using System.Threading.Tasks;
using AmazData.Module.Mqtt.Services;
using OrchardCore.DisplayManagement.Notify;
using Microsoft.AspNetCore.Mvc.Localization;
using AmazData.Module.Mqtt.Models;

namespace AmazData.Module.Mqtt.Controllers
{
    public class HomeController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IMqttConnectionManager _mqttConnectionManager;
        private readonly IMqttOptionsBuilderService _mqttOptionsBuilderService;
        private readonly INotifier _notifier;
        private readonly IHtmlLocalizer<HomeController> _localizer;
        public HomeController(
            IContentManager contentManager,
            IMqttConnectionManager mqttConnectionManager,
            IMqttOptionsBuilderService mqttOptionsBuilderService,
            INotifier notifier,
            IHtmlLocalizer<HomeController> localizer
            )
        {
            _contentManager = contentManager;
            _mqttConnectionManager = mqttConnectionManager;
            _mqttOptionsBuilderService = mqttOptionsBuilderService;
            _notifier = notifier;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(string brokerId)
        {
            var contentItem = await _contentManager.GetAsync(brokerId);

            if (contentItem is null)
            {
                return NotFound();
            }
            var brokerPart = contentItem.As<BrokerPart>();
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
    }
}
