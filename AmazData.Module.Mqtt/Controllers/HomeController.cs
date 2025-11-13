using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using System.Threading.Tasks;
using AmazData.Module.Mqtt.Services;
using OrchardCore.DisplayManagement.Notify;
using Microsoft.AspNetCore.Mvc.Localization;

namespace AmazData.Module.Mqtt.Controllers
{
    public class HomeController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IMqttConnectionManager _mqttConnectionManager;
        private readonly IMqttOptionsBuilderService _mqttOptionsBuilderService;
        private readonly INotifier _notifier;
        private readonly IHtmlLocalizer<SubscriptionController> _localizer;
        public HomeController(
            IContentManager contentManager,
            IMqttConnectionManager mqttConnectionManager,
            IMqttOptionsBuilderService mqttOptionsBuilderService,
            INotifier notifier,
            IHtmlLocalizer<SubscriptionController> localizer)
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

            if (contentItem == null)
            {
                return NotFound();
            }

            var options = await _mqttOptionsBuilderService.BuildOptionsAsync(brokerId);

            if (options != null)
            {
                await _mqttConnectionManager.ConnectAsync(brokerId, options);
            }
            await _notifier.SuccessAsync(_localizer["Broker Connected successfully."]);
            return RedirectToAction("List", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "Broker" });
        }
    }
}
